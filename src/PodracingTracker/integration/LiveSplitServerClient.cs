using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OWML.Common;
using OWML.ModHelper;

namespace PodracingTracker;

/// <summary>
/// Sends commands to LiveSplit over its TCP server (LiveSplit Server component).
/// Command protocol matches LiveSplit <c>CommandServer</c> (e.g. <c>starttimer</c>, <c>setcurrentsplitname</c>, <c>split</c>, <c>reset</c>).
/// </summary>
public sealed class LiveSplitServerClient : IDisposable
{
    private const int ConnectTimeoutMilliseconds = 1500;

    /// <summary>Segment count in <c>Content/Rules/Outer Wilds - Podracing.lss</c> (must match splits open in LiveSplit).</summary>
    private const int PodracingBundledSplitCount = 25;

    private readonly IModHelper _modHelper;
    private TcpClient _tcp;
    private NetworkStream _stream;
    private bool _connectFailureLogged;

    public LiveSplitServerClient(IModHelper modHelper)
    {
        _modHelper = modHelper;
    }

    /// <summary>
    /// Drop any open connection so the next command picks up host/port from config again.
    /// </summary>
    public void RefreshFromConfig()
    {
        Disconnect();
        _connectFailureLogged = false;
    }

    /// <summary>
    /// <c>reset</c>, optional bulk <c>setsplitname</c> for Podracing segment count, then <c>starttimer</c>.
    /// </summary>
    public void StartRun()
    {
        if (!CanSend())
            return;
        SendLine("reset");
        ApplyStartSplitNameWipe();
        SendLine("starttimer");
    }

    /// <summary>
    /// For each qualifying landing completed this takeoff, renames the current split then issues <c>split</c>.
    /// Takeoffs that do not complete a landing send no commands.
    /// </summary>
    public void NotifyTakeoffLandings(IReadOnlyList<string> landingLabelsCompletedThisTakeoff)
    {
        if (!CanSend())
            return;
        if (landingLabelsCompletedThisTakeoff == null || landingLabelsCompletedThisTakeoff.Count == 0)
            return;

        foreach (string label in landingLabelsCompletedThisTakeoff)
        {
            if (!string.IsNullOrWhiteSpace(label))
                SendLine($"setcurrentsplitname {label}");
            SendLine("split");
        }
    }

    /// <summary>
    /// Optional label for the final segment, then <c>split</c> to close the run.
    /// </summary>
    public void CompleteRun(string finalSplitLabel)
    {
        if (!CanSend())
            return;
        if (!string.IsNullOrWhiteSpace(finalSplitLabel))
            SendLine($"setcurrentsplitname {finalSplitLabel}");
        SendLine("split");
        SendLine("pause");
    }

    /// <summary>
    /// <c>reset</c> — clears the timer (used on disqualification / failed run).
    /// </summary>
    public void FailRun()
    {
        if (!CanSend())
            return;
        SendLine("reset");
    }

    public void Dispose()
    {
        Disconnect();
    }

    private bool CanSend()
    {
        if (!_modHelper.Config.GetSettingsValue<bool>("LiveSplit Enabled"))
            return false;
        return _modHelper.Config.GetSettingsValue<bool>("LiveSplit Auto Connect");
    }

    private bool VerboseLogging() => _modHelper.Config.GetSettingsValue<bool>("LiveSplit Verbose Logs");

    /// <summary>
    /// Sends <c>setsplitname</c> for indices <c>0 .. PodracingBundledSplitCount-1</c>.
    /// Config <c>LiveSplit Wipe Split Names At Start</c>: default <c>""</c> clears each segment name; any other value is used as the name for every segment.
    /// </summary>
    private void ApplyStartSplitNameWipe()
    {
        string wipe = _modHelper.Config.GetSettingsValue<string>("LiveSplit Wipe Split Names At Start") ?? string.Empty;
        for (int i = 0; i < PodracingBundledSplitCount; i++)
        {
            if (string.IsNullOrEmpty(wipe))
                SendLine($"setsplitname {i} ");
            else
                SendLine($"setsplitname {i} {wipe}");
        }
    }

    private void SendLine(string line)
    {
        if (!EnsureConnected())
            return;
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(line + "\r\n");
            _stream!.Write(bytes, 0, bytes.Length);
            if (VerboseLogging())
                _modHelper.Console.WriteLine($"LiveSplit -> {line}", MessageType.Info);
        }
        catch (Exception ex)
        {
            Disconnect();
            if (!_connectFailureLogged)
            {
                _modHelper.Console.WriteLine($"LiveSplit Server: send failed ({ex.Message}).", MessageType.Warning);
                _connectFailureLogged = true;
            }
        }
    }

    private bool EnsureConnected()
    {
        if (!CanSend())
            return false;
        if (_tcp?.Connected == true && _stream != null)
            return true;

        Disconnect();
        string host = _modHelper.Config.GetSettingsValue<string>("LiveSplit Host");
        if (string.IsNullOrWhiteSpace(host))
            host = "127.0.0.1";
        int port = _modHelper.Config.GetSettingsValue<int>("LiveSplit Port");

        try
        {
            _tcp = new TcpClient { NoDelay = true };
            IAsyncResult ar = _tcp.BeginConnect(host, port, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(ConnectTimeoutMilliseconds, false))
            {
                try
                {
                    _tcp.Close();
                }
                catch
                {
                    // ignore
                }
                _tcp = null;
                throw new TimeoutException($"Connect timed out after {ConnectTimeoutMilliseconds} ms.");
            }
            _tcp.EndConnect(ar);
            _stream = _tcp.GetStream();
            _connectFailureLogged = false;
            return true;
        }
        catch (Exception ex)
        {
            Disconnect();
            if (!_connectFailureLogged)
            {
                _modHelper.Console.WriteLine($"LiveSplit Server: connect to {host}:{port} failed ({ex.Message}).", MessageType.Warning);
                _connectFailureLogged = true;
            }
            return false;
        }
    }

    private void Disconnect()
    {
        try
        {
            _stream?.Dispose();
        }
        catch
        {
            // ignore
        }
        try
        {
            _tcp?.Close();
        }
        catch
        {
            // ignore
        }
        _stream = null;
        _tcp = null;
    }
}

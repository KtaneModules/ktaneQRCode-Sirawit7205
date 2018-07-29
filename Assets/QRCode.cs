using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ZXing;
using ZXing.QrCode;

public class QRCode : MonoBehaviour
{

    public KMAudio Audio;
    public KMNeedyModule Module;
    public KMSelectable[] btn;
    public MeshRenderer QR;

    private bool _isReady = false;
    private int QRMessage;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private string input = null;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
    }

    private void Awake()
    {
        Module.OnNeedyActivation += OnNeedyActivation;
        Module.OnNeedyDeactivation += OnNeedyDeactivation;
        Module.OnTimerExpired += OnTimerExpired;
        for (int i = 0; i < 10; i++)
        {
            int j = i;
            btn[i].OnInteract += delegate ()
            {
                HandlePress(j);
                return false;
            };
        }
    }

    void HandlePress(int n)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, btn[n].transform);
        btn[n].AddInteractionPunch();
        if (!_isReady) return;

        input += n.ToString();
        if(input.Length == QRMessage.ToString().Length)
        {
            Debug.LogFormat("[NeedyQRCode #{0}] Answered {1}, Expected {2}.", _moduleId, input, QRMessage);

            if(input == QRMessage.ToString())
            {
                Module.HandlePass();
                Debug.LogFormat("[NeedyQRCode #{0}] Answer correct! Module passed!", _moduleId);
            }
            else
            {
                Module.HandleStrike();
                Module.HandlePass();
                Debug.LogFormat("[NeedyQRCode #{0}] Answer incorrect! Strike!", _moduleId);
            }
            input = null;
            OnNeedyDeactivation();
        }
    }

    void OnNeedyActivation()
    {
        QRMessage = Random.Range(10000, 100000000);
        Debug.LogFormat("[NeedyQRCode #{0}] Number is {1}.", _moduleId, QRMessage);

        IBarcodeWriter writer = new BarcodeWriter { Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 256,
                Width = 256
            }
        };
        var qr = writer.Write(QRMessage.ToString());
        var encoded = new Texture2D(256, 256);

        encoded.SetPixels32(qr);
        encoded.Apply();

        QR.material.mainTexture = encoded;
        QR.enabled = true;

        _isReady = true;

    }

    void OnNeedyDeactivation()
    {
        Debug.LogFormat("[NeedyQRCode #{0}] Module deactivated.", _moduleId);
        QR.enabled = false;
        _isReady = false;
    }

    void OnTimerExpired()
    {
        Module.OnStrike();
        Debug.LogFormat("[NeedyQRCode #{0}] Timed out! Strike!", _moduleId);
        QR.enabled = false;
        _isReady = false;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Submit the answer with “!{0} submit (answer)”.";
#pragma warning restore 414

    KMSelectable[] ProcessTwitchCommand(string command)
    {
        var btns = new List<KMSelectable>();

        command = command.ToLowerInvariant().Trim();

        if (Regex.IsMatch(command, @"^submit \d+$"))
        {
            command = command.Substring(7).Trim();
            for (int i = 0; i < command.Length; i++)
            {
                btns.Add(btn[command[i] - '0']);
            }
            if (btns.Count > 0) return btns.ToArray();
        }
        return null;
    }
}

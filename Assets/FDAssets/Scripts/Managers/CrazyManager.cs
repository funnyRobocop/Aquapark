using CrazyGames;
using Fusion.Photon.Realtime;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[Tooltip("Manager that handles calling the Crazy Games SDK")]
public class CrazyManager : MonoBehaviour
{
    [SerializeField, Tooltip("Reference to the Fusion Network Manager.")]
    FusionNetworkManager fusionNetworkManager;

    [SerializeField, Tooltip("Reference to the TextMeshProUGUI object that will display the player's name.")]
    TextMeshProUGUI playerName;

    [SerializeField, Tooltip("Event invokved if the game was instantly joined through an invite.")]
    UnityEvent onInstantJoin;

    void Start()
    {
        if (!Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer)
            return;

        CrazySDK.Init(() =>
        {
            Debug.Log("CrazySDK initialized");

            SetPlayerName();
            CheckInstantMultiplayer();
        });
    }

    /// <summary>
    /// Sets the player name.
    /// </summary>
    private void SetPlayerName()
    {
        // This action is asynchronous, so when the user's name has been properly received, it will update the text display.
        CrazySDK.User.GetUser(user =>
        {
            if (user != null)
            {
                playerName.text = user.username;
                Debug.Log("Get user result: " + user);
            }
        });
    }

    /// <summary>
    /// Once CrazyGames SDK has been initialized, an instant join check is made.
    /// </summary>
    private void CheckInstantMultiplayer()
    {
        bool isInstant;
        string session, appVersion, region;

        // Checking Invite Parameters can throw errors, which can pause the uunity editor.
        if (!Application.isEditor)
        {
            // We check to see if the game is an instant join, which is used primarily for testing.
            isInstant = CrazySDK.Game.IsInstantMultiplayer;

            // These will all return empty strings if no key if found.
            session = CrazySDK.Game.GetInviteLinkParameter("session");
            appVersion = CrazySDK.Game.GetInviteLinkParameter("appVersion");
            region = CrazySDK.Game.GetInviteLinkParameter("region");
        }
        else
        {
            isInstant = false;

            // These will all return empty strings if no key if found.
            session = "";
            appVersion = "";
            region = "";
        }

        // If the game is detected to be an instant game, we start a new session
        if (isInstant)
        {
            onInstantJoin.Invoke();

            if (session == null)
            {
                session = System.Guid.NewGuid().ToString();
            }

            bool isInvite = true;
            if (string.IsNullOrEmpty(appVersion))
            {
                appVersion = PhotonAppSettings.Global.AppSettings.AppVersion;
                isInvite = false;
            }

            fusionNetworkManager.StartSession(true, isInvite, appVersion, region, session);

            return;
        }

        // If the game is detected to be an invite, we also start a session immediately
        if (!string.IsNullOrEmpty(appVersion))
        {
            onInstantJoin.Invoke();
            fusionNetworkManager.StartSession(false, true, appVersion, region, session);
        }
    }

    /// <summary>
    /// Creates an Invite Link and copies it to the clipboard so it can be shared.
    /// </summary>
    public static void InviteLink()
    {
        Dictionary<string, string> param = CreateInviteDictionary();

        var link = CrazySDK.Game.InviteLink(param);
        CrazySDK.Game.CopyToClipboard(link);
    }

    /// <summary>
    /// Shows the invite game on the CrazyGame website
    /// </summary>
    public static void ShowInviteButton()
    {
        Dictionary<string, string> param = CreateInviteDictionary();

        var link = CrazySDK.Game.ShowInviteButton(param);

        Debug.Log(link);
    }

    /// <summary>
    /// Creates the invite dictionary/
    /// Updates this to add custom functionality
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, string> CreateInviteDictionary()
    {
        Dictionary<string, string> param = new Dictionary<string, string>();       

        var runner = FusionNetworkManager.Runner;

        // If no NetworkRunner is found, a dictionary is still created, but it will not result in an instant join.
        if (runner == null)
        {
            Debug.LogWarning("No NetworkRunner is currently active.");

            param.Add("session", string.Empty);
            param.Add("region", string.Empty);
            param.Add("appVersion", string.Empty);

            return param;
        }

        param.Add("appVersion", PhotonAppSettings.Global.AppSettings.AppVersion);
        param.Add("session", runner.SessionInfo.Name);
        param.Add("region", runner.SessionInfo.Region);

        return param;
    }
}
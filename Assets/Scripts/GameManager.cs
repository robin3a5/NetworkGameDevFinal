using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private NetworkVariable<int> frameCounter = new NetworkVariable<int>(1);

    private NetworkVariable<bool> isGameStarted = new NetworkVariable<bool>(false);

    public NetworkList<PlayerPanelStruct> playerPanels = new NetworkList<PlayerPanelStruct>();

    public List<Transform> pinLocations;

    public GameObject pinPrefab;

    [SerializeField]
    private Scoreboard _scoreBoard;

    private List<ScoreboardPlayerPanel> scoreboardPanels;

    public ScoreboardPlayerPanel playerPanel;

    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            scoreboardPanels = new List<ScoreboardPlayerPanel>();
            SpawnPinsServerRpc();
            // Host doesn't count as a connected client so must manually call methods for add and refresh
            AddPlayerToList(OwnerClientId);
            RefreshPlayerPanels();
            NetworkManager.Singleton.OnClientConnectedCallback += AddPlayerToList;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
            // SpawnPlayers();
        }
        else
        {
            scoreboardPanels = new List<ScoreboardPlayerPanel>();
        }

        playerPanels.OnListChanged += ClientOnAllPlayersChanged;
    }

    private void AddPlayerToList(ulong clientId)
    {
        playerPanels.Add(
            new PlayerPanelStruct(
                clientId,
                IsHost && clientId != NetworkManager.Singleton.LocalClientId,
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                ""
            )
        );
    }

    private void AddPlayerPanel(PlayerPanelStruct playerPanelStruct)
    {
        ScoreboardPlayerPanel newPanel = Instantiate(playerPanel);
        newPanel.SetName($"{playerPanelStruct.clientId}");
        newPanel.SetFrame1(playerPanelStruct.txtFrame1.ToString());
        newPanel.SetFrame2(playerPanelStruct.txtFrame2.ToString());
        newPanel.SetFrame3(playerPanelStruct.txtFrame3.ToString());
        newPanel.SetFrame4(playerPanelStruct.txtFrame4.ToString());
        newPanel.SetFrame5(playerPanelStruct.txtFrame5.ToString());
        newPanel.SetFrame6(playerPanelStruct.txtFrame6.ToString());
        newPanel.SetFrame7(playerPanelStruct.txtFrame7.ToString());
        newPanel.SetFrame8(playerPanelStruct.txtFrame8.ToString());
        newPanel.SetFrame9(playerPanelStruct.txtFrame9.ToString());
        newPanel.SetFrame10(playerPanelStruct.txtFrame10.ToString());
        newPanel.ShowKick(playerPanelStruct.showVoteKick);
        newPanel.OnVoteKickPlayer += delegate
        {
            OnVoteKickPressed(playerPanelStruct.clientId);
        };
        scoreboardPanels.Add(newPanel);
        _scoreBoard.AddPlayerToScoreboard(newPanel);
    }

    void OnVoteKickPressed(ulong clientId)
    {
        InitiateVoteKickClientRpc(clientId);
    }

    [ClientRpc]
    void InitiateVoteKickClientRpc(ulong clientId)
    {
        Debug.Log($"Vote to kick {clientId} started ");
    }

    void OnClientDisconnect(ulong clientId)
    {
        int indexToRemove = -1;
        for (int i = 0; i < scoreboardPanels.Count; i++)
        {
            if (scoreboardPanels[i].GetName() == clientId.ToString())
            {
                scoreboardPanels[i].OnVoteKickPlayer -= delegate
                {
                    OnVoteKickPressed(clientId);
                };
                indexToRemove = i;
            }
        }
        if (indexToRemove != -1)
        {
            scoreboardPanels.RemoveAt(indexToRemove);
        }
        indexToRemove = -1;
        for (int i = 0; i < playerPanels.Count; i++)
        {
            if (playerPanels[i].clientId == clientId)
            {
                indexToRemove = i;
            }
        }
        if (indexToRemove != -1)
        {
            playerPanels.RemoveAt(indexToRemove);
        }
        // Update player panels on all clients
        RefreshPlayerPanels();
    }

    private void ClientOnAllPlayersChanged(NetworkListEvent<PlayerPanelStruct> changeEvent)
    {
        RefreshPlayerPanels();
    }

    private void RefreshPlayerPanels()
    {
        _scoreBoard.ResetScoreBoard();
        // foreach (var player in scoreboardPanels)
        // {
        //     ScoreboardPlayerPanel newPanel = Instantiate(playerPanel);
        //     newPanel.SetName(player.GetName());
        //     newPanel.SetFrame1(player.GetFrame1());
        //     newPanel.SetFrame2(player.GetFrame2());
        //     newPanel.SetFrame3(player.GetFrame3());
        //     newPanel.SetFrame4(player.GetFrame4());
        //     newPanel.SetFrame5(player.GetFrame5());
        //     newPanel.SetFrame6(player.GetFrame6());
        //     newPanel.SetFrame7(player.GetFrame7());
        //     newPanel.SetFrame8(player.GetFrame8());
        //     newPanel.SetFrame9(player.GetFrame9());
        //     newPanel.SetFrame10(player.GetFrame9());
        //     newPanel.ShowKick(player.GetShowVoteKick());
        //     newPanel.OnVoteKickPlayer += delegate
        //     {
        //         OnVoteKickPressed(ulong.Parse(player.GetName()));
        //     };
        //     _scoreBoard.AddPlayerToScoreboard(newPanel);
        // }
        foreach (PlayerPanelStruct panel in playerPanels)
        {
            AddPlayerPanel(panel);
        }
    }

    [ServerRpc]
    void SpawnPinsServerRpc()
    {
        foreach (var pin in pinLocations)
        {
            GameObject pinToSpawn = Instantiate(pinPrefab, pin.position, pin.rotation);
            pinToSpawn.gameObject.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}
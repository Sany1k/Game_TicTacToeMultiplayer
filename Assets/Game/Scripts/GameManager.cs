using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OnClickedOnGridPositionEventArgs : EventArgs
{
    public int x, y;
    public PlayerType playerType;
}
public class OnGameWinEventArgs : EventArgs
{
    public Line line;
    public PlayerType winPlayerType;
}
public enum PlayerType
{
    None,
    Cross,
    Circle
}
public enum Orientation
{
    Horizontal,
    Vertical,
    DiagonalA,
    DiagonalB
}
public struct Line
{
    public List<Vector2Int> gridVector2IntList;
    public Vector2Int centerGridPosition;
    public Orientation orientation;
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public event EventHandler OnGameStarted, OnRematch, OnGameTied;
    public event EventHandler OnCurrentPlayablePlayerTypeChanged, OnScoreChanged, OnPlacedObject;

    private PlayerType localPlayerType;
    private PlayerType[,] playerTypeArray;
    private List<Line> lineList;
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new();
    private NetworkVariable<int> playerCrossScore = new();
    private NetworkVariable<int> playerCircleScore = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager instance!");
        }

        Instance = this;
        playerTypeArray = new PlayerType[3, 3];
        lineList = new()
        {
            new Line
            {
                gridVector2IntList = new() { new(0, 0), new(1, 0), new(2, 0) },
                centerGridPosition = new(1, 0),
                orientation = Orientation.Horizontal
            },
            new Line
            {
                gridVector2IntList = new() { new(0, 1), new(1, 1), new(2, 1) },
                centerGridPosition = new(1, 1),
                orientation = Orientation.Horizontal
            }
            ,new Line
            {
                gridVector2IntList = new() { new(0, 2), new(1, 2), new(2, 2) },
                centerGridPosition = new(1, 2),
                orientation = Orientation.Horizontal
            },
            new Line
            {
                gridVector2IntList = new() { new(0, 0), new(0, 1), new(0, 2) },
                centerGridPosition = new(0, 1),
                orientation = Orientation.Vertical
            },
            new Line
            {
                gridVector2IntList = new() { new(1, 0), new(1, 1), new(1, 2) },
                centerGridPosition = new(1, 1),
                orientation = Orientation.Vertical
            },
            new Line
            {
                gridVector2IntList = new() { new(2, 0), new(2, 1), new(2, 2) },
                centerGridPosition = new(2, 1),
                orientation = Orientation.Vertical
            },
            new Line
            {
                gridVector2IntList = new() { new(0, 0), new(1, 1), new(2, 2) },
                centerGridPosition = new(1, 1),
                orientation = Orientation.DiagonalA
            },
            new Line
            {
                gridVector2IntList = new() { new(0, 2), new(1, 1), new(2, 0) },
                centerGridPosition = new(1, 1),
                orientation = Orientation.DiagonalB
            },
        };
    }

    public override void OnNetworkSpawn()
    {
        localPlayerType = (NetworkManager.Singleton.LocalClientId == 0) ? PlayerType.Cross : PlayerType.Circle;

        if (IsServer)
        {
            currentPlayablePlayerType.Value = PlayerType.Cross;
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) => 
        {
            OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        };
        playerCrossScore.OnValueChanged += (int preScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
        playerCircleScore.OnValueChanged += (int preScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        if (playerType != currentPlayablePlayerType.Value) return;
        if (playerTypeArray[x, y] != PlayerType.None) return;

        playerTypeArray[x, y] = playerType;
        TriggerOnPlacedObjectRpc();
        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs 
        { 
            x = x, y = y, 
            playerType = playerType
        });
        currentPlayablePlayerType.Value = (currentPlayablePlayerType.Value == PlayerType.Cross) ? PlayerType.Circle : PlayerType.Cross;

        TestWinner();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlacedObjectRpc()
    {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }

    private bool TestWinnerLine(Line line)
    {
        return TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]);
    }

    private bool TestWinnerLine(PlayerType a, PlayerType b, PlayerType c)
    {
        return a != PlayerType.None && a == b && b == c;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        Line line = lineList[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType
        });
    }

    private void TestWinner()
    {
        for (int i = 0; i < lineList.Count; i++)
        {
            Line line = lineList[i];

            if (TestWinnerLine(line))
            {
                currentPlayablePlayerType.Value = PlayerType.None;
                PlayerType winPlayerType = playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y];
                switch (winPlayerType)
                {
                    case PlayerType.Cross:
                        playerCrossScore.Value++;
                        break;
                    case PlayerType.Circle:
                        playerCircleScore.Value++;
                        break;
                }
                TriggerOnGameWinRpc(i, winPlayerType);
                return;
            }
        }

        bool hasTie = true;
        for (int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                if (playerTypeArray[x, y] == PlayerType.None)
                {
                    hasTie = false;
                    break;
                }
            }
        }
        if (hasTie)
        {
            TriggerOnGameTiedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for(int x = 0; x < playerTypeArray.GetLength(0); x++)
        {
            for (int y = 0; y < playerTypeArray.GetLength(1); y++)
            {
                playerTypeArray[x, y] = PlayerType.None;
            }
        }

        currentPlayablePlayerType.Value = PlayerType.Cross;
        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }

    public PlayerType GetLocalPlayerType() => localPlayerType;

    public PlayerType GetCurrentPlayablePlayerType() => currentPlayablePlayerType.Value;

    public void GetScores(out int playerCrossScore, out int playerCircleScore)
    {
        playerCrossScore = this.playerCrossScore.Value;
        playerCircleScore = this.playerCircleScore.Value;
    }

    private void NetworkManager_OnClientConnectedCallback(ulong id)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            // Start Game
            TriggerOnGameStartedRpc();
        }
    }

    private void OnApplicationQuit()
    {
        if (!Application.isEditor)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}

using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameVisualManager : NetworkBehaviour
{
    private const float GRID_SIZE = 3.1f;

    [SerializeField] Transform crossPrefab, circlePrefab, lineCompletePrefab;

    private List<GameObject> visualGameObjectList;

    private void Awake()
    {
        visualGameObjectList = new();
    }

    private void Start()
    {
        GameManager.Instance.OnClickedOnGridPosition += GameManager_OnClickedOnGridPosition;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
    }

    private Vector2 GetGridWorldPosition(int x, int y)
    {
        return new Vector2(-GRID_SIZE + x * GRID_SIZE, -GRID_SIZE + y * GRID_SIZE);
    }

    [Rpc(SendTo.Server)]
    private void SpawnObjectRpc(int x, int y, PlayerType playerType)
    {
        Transform prefab = (playerType == PlayerType.Cross) ? crossPrefab : circlePrefab;
        Transform spawnedCrossTranform = Instantiate(prefab, GetGridWorldPosition(x, y), Quaternion.identity);
        spawnedCrossTranform.GetComponent<NetworkObject>().Spawn(true);
        visualGameObjectList.Add(spawnedCrossTranform.gameObject);
    }

    private void GameManager_OnClickedOnGridPosition(object sender, OnClickedOnGridPositionEventArgs e)
    {
        SpawnObjectRpc(e.x, e.y, e.playerType);
    }

    private void GameManager_OnGameWin(object sender, OnGameWinEventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        float eulerZ = 0f;
        switch (e.line.orientation)
        {
            case Orientation.Horizontal: eulerZ = 0f; break;
            case Orientation.Vertical: eulerZ = 90f; break;
            case Orientation.DiagonalA: eulerZ = 45f; break;
            case Orientation.DiagonalB: eulerZ = -45f; break;
            default: break;
        }

        Transform lineCompleteTransform = Instantiate(
            lineCompletePrefab,
            GetGridWorldPosition(e.line.centerGridPosition.x, e.line.centerGridPosition.y),
            Quaternion.Euler(0, 0, eulerZ));
        lineCompleteTransform.GetComponent<NetworkObject>().Spawn(true);
        visualGameObjectList.Add(lineCompleteTransform.gameObject);
    }

    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        foreach (GameObject visualGameObject in visualGameObjectList)
        {
            Destroy(visualGameObject);
        }

        visualGameObjectList.Clear();
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    private const string TEXT_WIN = "YOU WIN!";
    private const string TEXT_LOSE = "YOU LOSE!";
    private const string TEXT_TIE = "TIE";

    [SerializeField] private TextMeshProUGUI resultTextMeshPro;
    [SerializeField] private Color winColor, loseColor, tieColor;
    [SerializeField] private Button rematchButton;

    private void Awake()
    {
        rematchButton.onClick.AddListener(() => {
            GameManager.Instance.RematchRpc();
        });
    }

    private void Start()
    {
        Hide();
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameObject_OnRematch;
        GameManager.Instance.OnGameTied += GameManager_OnGameTied;
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void GameManager_OnGameWin(object sender, OnGameWinEventArgs e)
    {
        if (e.winPlayerType == GameManager.Instance.GetLocalPlayerType())
        {
            resultTextMeshPro.text = TEXT_WIN;
            resultTextMeshPro.color = winColor;
        }
        else
        {
            resultTextMeshPro.text = TEXT_LOSE;
            resultTextMeshPro.color = loseColor;
        }

        Show();
    }

    private void GameObject_OnRematch(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void GameManager_OnGameTied(object sender, System.EventArgs e)
    {
        resultTextMeshPro.text = TEXT_TIE;
        resultTextMeshPro.color = tieColor;
        Show();
    }
}

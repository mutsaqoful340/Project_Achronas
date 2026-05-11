using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreen : MonoBehaviour
{
    public GameObject loadingPanel;
    public TextMeshProUGUI loadingText;
    public Image loadingBar; // optional
    public MonoBehaviour playerMovement;

    public MenuSelector menuSelector; // 🆕

    public bool isInitialLoad = false;

    public void StartLoading()
    {
        isInitialLoad = false;
        loadingPanel.SetActive(true);
        StartCoroutine(LoadAsync());
        menuSelector.DisableAll();
    }

    public void StartInitialLoading() // 🆕 dipanggil dari awal game
    {
        isInitialLoad = true;
        loadingPanel.SetActive(true);
        StartCoroutine(LoadAsync());
    }

    IEnumerator LoadAsync()
    {
        float progress = 0f;
        while (progress < 100f)
        {
            float speed;
            if (Mathf.FloorToInt(progress) == 23 ||
                Mathf.FloorToInt(progress) == 47 ||
                Mathf.FloorToInt(progress) == 68 ||
                Mathf.FloorToInt(progress) == 85)
            {
                speed = 8f;
            }
            else
            {
                speed = Random.Range(80f, 150f);
            }

            progress = Mathf.MoveTowards(progress, 100f, Time.unscaledDeltaTime * speed); // ← ubah ini
            loadingText.text = Mathf.FloorToInt(progress) + "%";
            if (loadingBar != null)
                loadingBar.fillAmount = progress / 100f;
            yield return null;
        }

        // reset timescale sebelum lanjut
        Time.timeScale = 1f; // ← tambahkan ini

        loadingText.text = "100%";
        yield return new WaitForSecondsRealtime(0.3f); // ← ubah ini juga

        if (isInitialLoad)
        {
            loadingPanel.SetActive(false);
            menuSelector.ShowMainMenu();
        }
        else
        {
            menuSelector.continuePanel.SetActive(false);
            menuSelector.DisableAll();
            loadingPanel.SetActive(false);
            playerMovement.enabled = true;
            menuSelector.isInContinuePanel = false; // sudah ada ini kan?
            Time.timeScale = 1f;                    // ← tambahkan ini
        }
    }
}
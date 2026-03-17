using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DistributedCookieClicker : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string apiUrl = "https://sid-restapi.onrender.com";

    [Header("Main Panels")]
    [SerializeField] private GameObject panelAuth;
    [SerializeField] private GameObject panelGame;
    [SerializeField] private GameObject panelLeaderboard;

    [Header("Auth SubPanels")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelRegister;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginUsernameInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button goToRegisterButton;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField registerUsernameInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private Button goToLoginButton;

    [Header("Shared UI")]
    [SerializeField] private TMP_Text statusText;

    [Header("Game UI")]
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private TMP_Text localScoreText;
    [SerializeField] private TMP_Text serverScoreText;
    [SerializeField] private Button cookieButton;
    [SerializeField] private Button saveScoreButton;
    [SerializeField] private Button logoutButton;
    [SerializeField] private Button showLeaderboardButton;

    [Header("Leaderboard UI")]
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private Button closeLeaderboardButton;

    [Header("Cookie Animation")]
    [SerializeField] private RectTransform cookieTransform;
    [SerializeField] private float clickScaleMultiplier = 1.08f;
    [SerializeField] private float clickAnimDuration = 0.08f;

    private string token;
    private string username;

    private int localScore = 0;
    private int serverScore = 0;

    private Coroutine cookieAnimCoroutine;

    private void Start()
    {
        HookButtons();

        token = PlayerPrefs.GetString("Token", "");
        username = PlayerPrefs.GetString("Username", "");

        panelAuth.SetActive(true);
        panelGame.SetActive(false);
        panelLeaderboard.SetActive(false);

        panelLogin.SetActive(true);
        panelRegister.SetActive(false);

        UpdateScoreTexts();
        SetStatus("Inicia sesión o regístrate.");

        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
        {
            SetStatus("Verificando sesión...");
            StartCoroutine(GetProfileCoroutine());
        }
    }

    private void HookButtons()
    {
        if (loginButton != null) loginButton.onClick.AddListener(OnLoginButton);
        if (goToRegisterButton != null) goToRegisterButton.onClick.AddListener(ShowRegisterPanel);

        if (registerButton != null) registerButton.onClick.AddListener(OnRegisterButton);
        if (goToLoginButton != null) goToLoginButton.onClick.AddListener(ShowLoginPanel);

        if (cookieButton != null) cookieButton.onClick.AddListener(OnCookieClick);
        if (saveScoreButton != null) saveScoreButton.onClick.AddListener(OnSaveScoreButton);
        if (logoutButton != null) logoutButton.onClick.AddListener(OnLogoutButton);
        if (showLeaderboardButton != null) showLeaderboardButton.onClick.AddListener(OnShowLeaderboardButton);

        if (closeLeaderboardButton != null) closeLeaderboardButton.onClick.AddListener(OnCloseLeaderboardButton);
    }

    // =========================
    // AUTH PANELS
    // =========================

    public void ShowLoginPanel()
    {
        panelLogin.SetActive(true);
        panelRegister.SetActive(false);
        SetStatus("Inicia sesión.");
    }

    public void ShowRegisterPanel()
    {
        panelLogin.SetActive(false);
        panelRegister.SetActive(true);
        SetStatus("Crea una cuenta nueva.");
    }

    // =========================
    // AUTH
    // =========================

    public void OnRegisterButton()
    {
        string user = registerUsernameInput.text.Trim();
        string pass = registerPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetStatus("Completa usuario y contraseña para registrarte.");
            return;
        }

        StartCoroutine(RegisterCoroutine(user, pass));
    }

    public void OnLoginButton()
    {
        string user = loginUsernameInput.text.Trim();
        string pass = loginPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            SetStatus("Completa usuario y contraseña para iniciar sesión.");
            return;
        }

        StartCoroutine(LoginCoroutine(user, pass));
    }

    private IEnumerator RegisterCoroutine(string user, string pass)
    {
        SetStatus("Registrando usuario...");

        RegisterRequest body = new RegisterRequest
        {
            username = user,
            password = pass
        };

        string json = JsonUtility.ToJson(body);

        UnityWebRequest req = new UnityWebRequest(apiUrl + "/api/usuarios", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Register error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("Registro fallido: " + SafeServerMessage(req));
            yield break;
        }

        SetStatus("Registro exitoso. Ahora inicia sesión.");
        ShowLoginPanel();

        loginUsernameInput.text = user;
        loginPasswordInput.text = "";
        registerPasswordInput.text = "";
    }

    private IEnumerator LoginCoroutine(string user, string pass)
    {
        SetStatus("Iniciando sesión...");

        LoginRequest body = new LoginRequest
        {
            username = user,
            password = pass
        };

        string json = JsonUtility.ToJson(body);

        UnityWebRequest req = new UnityWebRequest(apiUrl + "/api/auth/login", "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Login error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("Login fallido: " + SafeServerMessage(req));
            yield break;
        }

        LoginResponse response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);

        if (response == null || response.usuario == null || string.IsNullOrEmpty(response.token))
        {
            SetStatus("La respuesta del login no se pudo interpretar.");
            yield break;
        }

        token = response.token;
        username = response.usuario.username;

        PlayerPrefs.SetString("Token", token);
        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.Save();

        StartCoroutine(GetProfileCoroutine());
    }

    private IEnumerator GetProfileCoroutine()
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
        {
            SetLoggedOutUI();
            SetStatus("No hay sesión válida.");
            yield break;
        }

        UnityWebRequest req = UnityWebRequest.Get(apiUrl + "/api/usuarios/" + username);
        req.SetRequestHeader("x-token", token);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("GetProfile error: " + req.error + " | " + req.downloadHandler.text);
            ClearSession();
            SetLoggedOutUI();
            SetStatus("Sesión expirada o token inválido.");
            yield break;
        }

        UserResponse response = JsonUtility.FromJson<UserResponse>(req.downloadHandler.text);

        if (response == null || response.usuario == null)
        {
            ClearSession();
            SetLoggedOutUI();
            SetStatus("No se pudo leer el perfil.");
            yield break;
        }

        username = response.usuario.username;
        serverScore = (response.usuario.data != null) ? response.usuario.data.score : 0;
        localScore = serverScore;

        PlayerPrefs.SetString("Username", username);
        PlayerPrefs.Save();

        SetLoggedInUI();
        UpdateScoreTexts();
        SetStatus("Sesión activa.");
    }

    public void OnLogoutButton()
    {
        ClearSession();
        SetLoggedOutUI();
        SetStatus("Sesión cerrada.");
    }

    private void ClearSession()
    {
        token = "";
        username = "";

        PlayerPrefs.DeleteKey("Token");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.Save();

        localScore = 0;
        serverScore = 0;
        UpdateScoreTexts();
    }

    // =========================
    // GAME
    // =========================

    public void OnCookieClick()
    {
        if (!IsAuthenticated())
        {
            SetStatus("Debes iniciar sesión.");
            return;
        }

        localScore++;
        UpdateScoreTexts();

        if (cookieTransform != null)
        {
            if (cookieAnimCoroutine != null) StopCoroutine(cookieAnimCoroutine);
            cookieAnimCoroutine = StartCoroutine(CookieClickAnimation());
        }
    }

    private IEnumerator CookieClickAnimation()
    {
        Vector3 originalScale = cookieTransform.localScale;
        Vector3 targetScale = originalScale * clickScaleMultiplier;

        float t = 0f;
        while (t < clickAnimDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / clickAnimDuration);
            cookieTransform.localScale = Vector3.Lerp(originalScale, targetScale, p);
            yield return null;
        }

        t = 0f;
        while (t < clickAnimDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / clickAnimDuration);
            cookieTransform.localScale = Vector3.Lerp(targetScale, originalScale, p);
            yield return null;
        }

        cookieTransform.localScale = originalScale;
    }

    public void OnSaveScoreButton()
    {
        if (!IsAuthenticated())
        {
            SetStatus("Debes iniciar sesión.");
            return;
        }

        StartCoroutine(UpdateScoreCoroutine(localScore));
    }

    private IEnumerator UpdateScoreCoroutine(int newScore)
    {
        SetStatus("Guardando score...");

        UpdateUserRequest body = new UpdateUserRequest
        {
            username = username,
            data = new UserData
            {
                score = newScore
            }
        };

        string json = JsonUtility.ToJson(body);

        UnityWebRequest req = new UnityWebRequest(apiUrl + "/api/usuarios", "PATCH");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("x-token", token);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Update score error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("No se pudo guardar el score: " + SafeServerMessage(req));
            yield break;
        }

        serverScore = newScore;
        UpdateScoreTexts();
        SetStatus("Score guardado correctamente.");
    }

    // =========================
    // LEADERBOARD
    // =========================

    public void OnShowLeaderboardButton()
    {
        if (!IsAuthenticated())
        {
            SetStatus("Debes iniciar sesión.");
            return;
        }

        StartCoroutine(GetUsersCoroutine());
    }

    public void OnCloseLeaderboardButton()
    {
        panelLeaderboard.SetActive(false);
        panelGame.SetActive(true);
    }

    private IEnumerator GetUsersCoroutine()
    {
        SetStatus("Cargando leaderboard...");

        string url = apiUrl + "/api/usuarios?limit=100&skip=0&sort=true";
        UnityWebRequest req = UnityWebRequest.Get(url);
        req.SetRequestHeader("x-token", token);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Get users error: " + req.error + " | " + req.downloadHandler.text);
            SetStatus("Error cargando usuarios: " + SafeServerMessage(req));
            yield break;
        }

        UsersResponse response = JsonUtility.FromJson<UsersResponse>(req.downloadHandler.text);

        if (response == null || response.usuarios == null)
        {
            SetStatus("No se pudo leer la lista de usuarios.");
            yield break;
        }

        UserModel[] users = response.usuarios;

        Array.Sort(users, (a, b) =>
        {
            int scoreA = (a != null && a.data != null) ? a.data.score : 0;
            int scoreB = (b != null && b.data != null) ? b.data.score : 0;
            return scoreB.CompareTo(scoreA);
        });

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("TABLA DE PUNTAJES");
        sb.AppendLine("--------------------");

        int top = Mathf.Min(10, users.Length);

        for (int i = 0; i < top; i++)
        {
            string user = (users[i] != null && !string.IsNullOrEmpty(users[i].username)) ? users[i].username : "sin_nombre";
            int score = (users[i] != null && users[i].data != null) ? users[i].data.score : 0;
            sb.AppendLine($"{i + 1}. {user} - {score}");
        }

        leaderboardText.text = sb.ToString();
        panelGame.SetActive(false);
        panelLeaderboard.SetActive(true);

        SetStatus("Leaderboard cargado.");
    }

    // =========================
    // UI / HELPERS
    // =========================

    private bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username);
    }

    private void SetLoggedInUI()
    {
        panelAuth.SetActive(false);
        panelGame.SetActive(true);
        panelLeaderboard.SetActive(false);

        panelLogin.SetActive(true);
        panelRegister.SetActive(false);

        welcomeText.text = "Jugador: " + username;
    }

    private void SetLoggedOutUI()
    {
        panelAuth.SetActive(true);
        panelGame.SetActive(false);
        panelLeaderboard.SetActive(false);

        panelLogin.SetActive(true);
        panelRegister.SetActive(false);

        welcomeText.text = "Jugador: -";
        leaderboardText.text = "";
        UpdateScoreTexts();
    }

    private void UpdateScoreTexts()
    {
        if (localScoreText != null) localScoreText.text = "Score local: " + localScore;
        if (serverScoreText != null) serverScoreText.text = "Score guardado: " + serverScore;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private string SafeServerMessage(UnityWebRequest req)
    {
        if (req == null || req.downloadHandler == null) return "sin detalles";
        string text = req.downloadHandler.text;
        if (string.IsNullOrEmpty(text)) return req.error;
        return text;
    }
}

[Serializable]
public class RegisterRequest
{
    public string username;
    public string password;
}

[Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[Serializable]
public class UpdateUserRequest
{
    public string username;
    public UserData data;
}

[Serializable]
public class UserData
{
    public int score;
}

[Serializable]
public class UserModel
{
    public string _id;
    public string username;
    public bool state;
    public UserData data;
}

[Serializable]
public class LoginResponse
{
    public UserModel usuario;
    public string token;
}

[Serializable]
public class UserResponse
{
    public UserModel usuario;
}

[Serializable]
public class UsersResponse
{
    public UserModel[] usuarios;
}
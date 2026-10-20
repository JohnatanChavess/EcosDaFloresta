using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecos
{
    /// <summary>
    /// Autoteste opcional. Execute o jogo com "-selftest" para carregar cada cena, simular
    /// alguns segundos de jogo (andando, correndo e pulando) e registrar erros no log.
    /// Com "-screenshots &lt;pasta&gt;" também salva capturas de tela de cada cena.
    /// </summary>
    public class SelfTestRunner : MonoBehaviour
    {
        int errorCount;
        string screenshotFolder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            bool selfTest = false;
            string shots = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-selftest") selfTest = true;
                if (args[i] == "-screenshots" && i + 1 < args.Length) shots = args[i + 1];
            }
            if (!selfTest) return;
            var go = new GameObject("SelfTestRunner");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<SelfTestRunner>();
            runner.screenshotFolder = shots;
        }

        void OnEnable() => Application.logMessageReceived += OnLog;
        void OnDisable() => Application.logMessageReceived -= OnLog;

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errorCount++;
                Debug.Log("[SELFTEST] Erro capturado: " + condition);
            }
        }

        IEnumerator Start()
        {
            if (!string.IsNullOrEmpty(screenshotFolder)) Directory.CreateDirectory(screenshotFolder);
            Debug.Log("[SELFTEST] Iniciando autoteste");
            // O teste altera recorde e fases liberadas: guarda o progresso real para restaurar no fim.
            string[] keys = { "ecos_unlocked_level", "ecos_best_score", "ecos_has_save" };
            var saved = new int[keys.Length];
            var existed = new bool[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                existed[i] = PlayerPrefs.HasKey(keys[i]);
                saved[i] = PlayerPrefs.GetInt(keys[i], 0);
            }

            yield return new WaitForSecondsRealtime(2.5f);
            yield return Capture("01_menu_principal");

            string[] scenes = { GameConstants.SceneLevel1, GameConstants.SceneLevel2, GameConstants.SceneLevel3 };
            for (int i = 0; i < scenes.Length; i++)
            {
                SceneManager.LoadScene(scenes[i]);
                yield return new WaitForSecondsRealtime(1.0f);
                yield return Capture((i + 2).ToString("00") + "_fase" + (i + 1) + "_inicio");
                yield return SimulatePlay(6f);
                yield return Capture((i + 2).ToString("00") + "_fase" + (i + 1) + "_jogando");
                var gm = GameManager.Instance;
                Debug.Log("[SELFTEST] " + scenes[i] + " ok. Estado=" + (gm != null ? gm.CurrentState.ToString() : "sem GameManager") +
                          " Vidas=" + (gm != null ? gm.Lives : -1) + " Cristais=" + (gm != null ? gm.Crystals + "/" + gm.TotalCrystals : "-") +
                          " X=" + (gm != null && gm.Player != null ? gm.Player.transform.position.x.ToString("0.0") : "-"));
                if (gm != null && i == 0)
                {
                    gm.Pause();
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Capture("05_menu_pausa");
                    gm.Resume();
                }
            }

            yield return FunctionalTests();

            InputReader.Simulated = false;
            SceneManager.LoadScene(GameConstants.SceneVictory);
            yield return new WaitForSecondsRealtime(2f);
            yield return Capture("09_vitoria");

            bool ok = errorCount == 0 && failedChecks == 0;
            Debug.Log("[SELFTEST] RESULTADO: " + (ok ? "SUCESSO" : "FALHOU") + " (" + errorCount + " erros, " +
                      passedChecks + " verificações ok, " + failedChecks + " falharam)");
            for (int i = 0; i < keys.Length; i++)
            {
                if (existed[i]) PlayerPrefs.SetInt(keys[i], saved[i]);
                else PlayerPrefs.DeleteKey(keys[i]);
            }
            PlayerPrefs.Save();
            Application.Quit(ok ? 0 : 1);
        }

        // ------------------------------------------------------------------ Testes funcionais

        int passedChecks;
        int failedChecks;

        void Check(string name, bool condition, string detail = "")
        {
            if (condition) passedChecks++;
            else failedChecks++;
            Debug.Log("[SELFTEST] " + (condition ? "OK    " : "FALHOU") + " - " + name + (string.IsNullOrEmpty(detail) ? "" : " (" + detail + ")"));
        }

        static GameManager GM => GameManager.Instance;
        static Vector3 PlayerPos => GM.Player.transform.position;

        static void Teleport(float x, float y) => GM.Player.TeleportTo(new Vector2(x, y));

        static void SetInput(float h, float v, bool jump = false)
        {
            InputReader.Simulated = true;
            InputReader.SimHorizontal = h;
            InputReader.SimVertical = v;
            InputReader.SimJumpPressed = jump;
            InputReader.SimJumpHeld = jump;
            InputReader.SimRun = false;
        }

        static IEnumerator Hold(float h, float v, float seconds)
        {
            SetInput(h, v);
            yield return new WaitForSecondsRealtime(seconds);
            SetInput(0f, 0f);
        }

        static IEnumerator LoadLevel(string scene)
        {
            SetInput(0f, 0f);
            SceneManager.LoadScene(scene);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        IEnumerator FunctionalTests()
        {
            // ---------------- Fase 1: inimigo, espinhos, checkpoint, buraco, portal
            yield return LoadLevel(GameConstants.SceneLevel1);
            PatrolEnemy slime = null;
            foreach (var e in FindObjectsByType<PatrolEnemy>(FindObjectsSortMode.None))
                if (slime == null || Mathf.Abs(e.transform.position.x - 55f) < Mathf.Abs(slime.transform.position.x - 55f)) slime = e;
            int scoreBefore = GameSession.Score;
            Teleport(slime.transform.position.x, slime.transform.position.y + 2.2f);
            yield return new WaitForSecondsRealtime(1.0f);
            Check("Pisão derrota o Lodo Sombrio", slime == null && GM.Lives == 3 && GameSession.Score > scoreBefore,
                "vidas=" + GM.Lives + " pontos=" + GameSession.Score);

            Teleport(104.5f, 3.4f);
            yield return new WaitForSecondsRealtime(0.4f);
            Check("Espinhos causam dano e empurram", GM.Lives == 2, "vidas=" + GM.Lives);
            yield return new WaitForSecondsRealtime(1.8f);

            Teleport(81.5f, 3.05f);
            yield return new WaitForSecondsRealtime(0.4f);
            var cp = FindAnyObjectByType<Checkpoint>();
            Check("Checkpoint ativa ao toque", cp != null && cp.IsActive);

            Teleport(95.5f, 2f);
            yield return new WaitForSecondsRealtime(2.2f);
            Check("Queda no buraco tira vida e volta ao checkpoint", GM.Lives == 1 && Mathf.Abs(PlayerPos.x - 81.5f) < 1f,
                "vidas=" + GM.Lives + " x=" + PlayerPos.x.ToString("0.0"));
            yield return new WaitForSecondsRealtime(1.8f);

            int crystals = GM.Crystals;
            Teleport(8.5f, 3.05f);
            yield return new WaitForSecondsRealtime(0.3f);
            Check("Cristal coletado atualiza contador", GM.Crystals > crystals, GM.Crystals + "/" + GM.TotalCrystals);

            Teleport(174f, 5.05f);
            yield return new WaitForSecondsRealtime(2.5f);
            Check("Portal conclui a fase 1", GM.CurrentState == GameManager.State.LevelComplete);
            yield return Capture("06_fase_concluida");

            // ---------------- Fase 2: escada e plataforma móvel
            yield return LoadLevel(GameConstants.SceneLevel2);
            Teleport(10.6f, 3.05f);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Hold(0.6f, 0f, 0.15f);
            yield return Hold(0f, 1f, 3.2f);
            Check("Escalar a escada até o topo", PlayerPos.y > 9.5f && GM.Player.IsGrounded, "y=" + PlayerPos.y.ToString("0.00"));
            yield return Hold(1f, 0f, 0.6f);
            Check("Sair do topo da escada para o penhasco", PlayerPos.x > 12f && PlayerPos.y > 9.5f, "x=" + PlayerPos.x.ToString("0.0"));

            Teleport(103.5f, 9f);
            yield return Hold(0f, 1f, 0.2f);
            yield return Hold(0f, 1f, 1.8f);
            Check("Agarrar escada no ar e subir", PlayerPos.y > 13.5f, "y=" + PlayerPos.y.ToString("0.00"));

            var mover = FindAnyObjectByType<MovingPlatform>();
            Teleport(mover.transform.position.x, mover.transform.position.y + 0.3f);
            float startX = PlayerPos.x;
            yield return new WaitForSecondsRealtime(1.4f);
            Check("Plataforma móvel carrega o Lumo", PlayerPos.y > 5.6f && Mathf.Abs(PlayerPos.x - startX) > 1f,
                "dx=" + (PlayerPos.x - startX).ToString("0.0") + " y=" + PlayerPos.y.ToString("0.00"));

            // ---------------- Fase 3: bloco que desmorona, Game Over e Cristal Primordial
            yield return LoadLevel(GameConstants.SceneLevel3);
            Teleport(16.5f, 4.1f);
            yield return new WaitForSecondsRealtime(1.3f);
            Check("Bloco rachado desmorona sob o Lumo", PlayerPos.y < 3.5f || GM.Lives < 3, "y=" + PlayerPos.y.ToString("0.00") + " vidas=" + GM.Lives);

            yield return LoadLevel(GameConstants.SceneLevel1);
            for (int i = 0; i < 3; i++)
            {
                Teleport(95.5f, 2f);
                yield return new WaitForSecondsRealtime(2.3f);
            }
            yield return new WaitForSecondsRealtime(1.2f);
            Check("Perder as 3 vidas mostra Game Over", GM.CurrentState == GameManager.State.GameOver);
            yield return Capture("07_game_over");

            yield return LoadLevel(GameConstants.SceneLevel3);
            Teleport(211.5f, 7.05f);
            yield return new WaitForSecondsRealtime(0.5f);
            Check("Cristal Primordial conclui o jogo", GM != null && GM.CurrentState == GameManager.State.LevelComplete);
            yield return Capture("08_cristal_primordial");
            yield return new WaitForSecondsRealtime(5f);
            Check("Tela de vitória carregada", SceneManager.GetActiveScene().name == GameConstants.SceneVictory);
        }

        IEnumerator SimulatePlay(float seconds)
        {
            InputReader.Simulated = true;
            float t = 0f;
            while (t < seconds)
            {
                InputReader.SimHorizontal = 1f;
                InputReader.SimRun = t > 2f;
                bool jumpFrame = Mathf.Repeat(t, 0.9f) < Time.unscaledDeltaTime * 1.5f;
                InputReader.SimJumpPressed = jumpFrame;
                InputReader.SimJumpHeld = Mathf.Repeat(t, 0.9f) < 0.4f;
                InputReader.SimVertical = 0f;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            InputReader.SimHorizontal = 0f;
            InputReader.SimJumpPressed = false;
            InputReader.SimJumpHeld = false;
            InputReader.SimRun = false;
            InputReader.Simulated = false;
        }

        IEnumerator Capture(string name)
        {
            if (string.IsNullOrEmpty(screenshotFolder)) yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(screenshotFolder, name + ".png"));
            yield return null;
            yield return null;
        }
    }
}

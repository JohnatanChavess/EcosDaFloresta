using UnityEngine;
using UnityEngine.UI;
using static Ecos.EditorTools.UIFactory;

namespace Ecos.EditorTools
{
    /// <summary>Monta as telas do jogo: HUD + painéis da fase, menu principal e vitória.</summary>
    public static class UIBuilder
    {
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        // ================================================================== HUD DA FASE

        public static GameObject CreateLevelCanvasPrefab()
        {
            var canvas = CreateCanvas("LevelCanvas", 10);
            var root = canvas.transform;
            var hudGo = Node(root, "HUD");
            Stretch(hudGo);
            var hud = hudGo.gameObject.AddComponent<HUDController>();

            // Flash de dano (atrás de tudo do HUD).
            var flash = Image(hudGo, "FlashDano", null, new Color(1f, 0.15f, 0.2f, 0f));
            Stretch(flash.rectTransform);

            // Vidas.
            var heartFull = AssetGenerator.Sprite("ui_heart_full");
            var heartEmpty = AssetGenerator.Sprite("ui_heart_empty");
            var hearts = new Image[GameConstants.MaxLives];
            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i] = Image(hudGo, "Coracao" + (i + 1), heartFull, Color.white);
                Place(hearts[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20 + i * 52, -16), new Vector2(48, 48));
            }

            // Cristais.
            var crystalIcon = Image(hudGo, "IconeCristal", AssetGenerator.Sprite("ui_crystal"), Color.white);
            Place(crystalIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(44, -96), new Vector2(48, 48));
            var crystalText = Label(hudGo, "TextoCristais", "0 / 0", 30, Cream, TextAnchor.MiddleLeft);
            Place(crystalText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(76, -96), new Vector2(220, 44));

            // Pontuação.
            var scoreLabel = Label(hudGo, "RotuloPontos", "PONTOS", 20, Mint, TextAnchor.UpperRight);
            Place(scoreLabel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -14), new Vector2(300, 26));
            var scoreText = Label(hudGo, "TextoPontos", "000000", 40, Gold, TextAnchor.UpperRight);
            Place(scoreText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24, -38), new Vector2(300, 48));

            // Nome da fase.
            var levelBg = Image(hudGo, "FundoFase", null, new Color(0.08f, 0.06f, 0.15f, 0.55f));
            Place(levelBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(520, 40));
            var levelText = Label(levelBg.transform, "TextoFase", "FASE 1", 22, Cream);
            Stretch(levelText.rectTransform);

            // Dicas.
            var hint = Panel(hudGo, "Dica", new Vector2(820, 96));
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 22), new Vector2(820, 96));
            hint.raycastTarget = false;
            var hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;
            var hintText = Label(hint.transform, "TextoDica", "", 24, Cream);
            Stretch(hintText.rectTransform);
            hintText.rectTransform.offsetMin = new Vector2(28, 12);
            hintText.rectTransform.offsetMax = new Vector2(-28, -12);

            // Faixa de título da fase.
            var banner = Panel(hudGo, "FaixaTitulo", new Vector2(760, 170));
            banner.raycastTarget = false;
            Place(banner.rectTransform, Center, Center, new Vector2(0, 90), new Vector2(760, 170));
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false;
            var bannerTitle = Label(banner.transform, "Titulo", "FASE 1", 56, Gold);
            Place(bannerTitle.rectTransform, Center, Center, new Vector2(0, 26), new Vector2(720, 70));
            var bannerSub = Label(banner.transform, "Subtitulo", "", 30, Mint);
            Place(bannerSub.rectTransform, Center, Center, new Vector2(0, -34), new Vector2(720, 44));

            // Textos flutuantes.
            var popups = Node(hudGo, "Popups");
            Stretch(popups);
            var popupTemplate = Label(popups, "ModeloPopup", "+100", 30, Gold);
            popupTemplate.rectTransform.sizeDelta = new Vector2(240, 44);

            EditorUtil.SetArray(hud, "hearts", hearts);
            EditorUtil.Set(hud, "heartFull", heartFull);
            EditorUtil.Set(hud, "heartEmpty", heartEmpty);
            EditorUtil.Set(hud, "crystalText", crystalText);
            EditorUtil.Set(hud, "crystalIcon", crystalIcon.rectTransform);
            EditorUtil.Set(hud, "scoreText", scoreText);
            EditorUtil.Set(hud, "levelText", levelText);
            EditorUtil.Set(hud, "hintGroup", hintGroup);
            EditorUtil.Set(hud, "hintText", hintText);
            EditorUtil.Set(hud, "bannerGroup", bannerGroup);
            EditorUtil.Set(hud, "bannerTitle", bannerTitle);
            EditorUtil.Set(hud, "bannerSubtitle", bannerSub);
            EditorUtil.Set(hud, "damageFlash", flash);
            EditorUtil.Set(hud, "popupTemplate", popupTemplate);
            EditorUtil.Set(hud, "popupContainer", popups);

            // ---------------- Painéis
            var ui = canvas.gameObject.AddComponent<LevelUI>();
            BuildPausePanel(root, ui);
            BuildGameOverPanel(root, ui);
            BuildLevelCompletePanel(root, ui);

            return EditorUtil.SavePrefab(canvas.gameObject, "Assets/Prefabs/UI/LevelCanvas.prefab");
        }

        static void BuildPausePanel(Transform root, LevelUI ui)
        {
            var group = Node(root, "PainelPausa");
            Stretch(group);
            Dimmer(group, new Color(0.03f, 0.02f, 0.08f, 0.65f));
            var panel = Panel(group, "Painel", new Vector2(480, 560));
            var title = Label(panel.transform, "Titulo", "PAUSADO", 52, Gold);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(440, 64));
            var list = VerticalGroup(panel.transform, "Botoes", new Vector2(0, -20), new Vector2(360, 360), 12);
            var resume = Button(list, "Continuar", "CONTINUAR", new Vector2(340, 58));
            var restart = Button(list, "Reiniciar", "REINICIAR FASE", new Vector2(340, 58));
            var sound = Button(list, "Som", "SOM: LIGADO", new Vector2(340, 58));
            var menu = Button(list, "Menu", "MENU PRINCIPAL", new Vector2(340, 58));
            var quit = Button(list, "Sair", "SAIR DO JOGO", new Vector2(340, 58));
            LinkVertical(resume, restart, sound, menu, quit);
            var tip = Label(panel.transform, "Dica", "ESC / P: continuar   •   M: som", 18, Mint);
            Place(tip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(440, 26));

            EditorUtil.Set(ui, "pausePanel", group.gameObject);
            EditorUtil.Set(ui, "resumeButton", resume);
            EditorUtil.Set(ui, "pauseRestartButton", restart);
            EditorUtil.Set(ui, "pauseSoundButton", sound);
            EditorUtil.Set(ui, "pauseMenuButton", menu);
            EditorUtil.Set(ui, "pauseQuitButton", quit);
        }

        static void BuildGameOverPanel(Transform root, LevelUI ui)
        {
            var group = Node(root, "PainelDerrota");
            Stretch(group);
            Dimmer(group, new Color(0.15f, 0.02f, 0.06f, 0.72f));
            var panel = Panel(group, "Painel", new Vector2(600, 520));
            var title = Label(panel.transform, "Titulo", "FIM DE JOGO", 58, new Color32(0xFF, 0x6B, 0x7D, 0xFF));
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(560, 70));
            var sub = Label(panel.transform, "Subtitulo", "As criaturas corrompidas venceram desta vez...", 22, Cream);
            Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -108), new Vector2(540, 30));
            var stats = Label(panel.transform, "Estatisticas", "", 26, Mint);
            Place(stats.rectTransform, Center, Center, new Vector2(0, 40), new Vector2(520, 120));
            var list = VerticalGroup(panel.transform, "Botoes", new Vector2(0, -150), new Vector2(380, 200), 12);
            var retry = Button(list, "TentarNovamente", "TENTAR NOVAMENTE", new Vector2(360, 58));
            var menu = Button(list, "Menu", "MENU PRINCIPAL", new Vector2(360, 58));
            var quit = Button(list, "Sair", "SAIR DO JOGO", new Vector2(360, 58));
            LinkVertical(retry, menu, quit);

            EditorUtil.Set(ui, "gameOverPanel", group.gameObject);
            EditorUtil.Set(ui, "gameOverStats", stats);
            EditorUtil.Set(ui, "retryButton", retry);
            EditorUtil.Set(ui, "gameOverMenuButton", menu);
            EditorUtil.Set(ui, "gameOverQuitButton", quit);
        }

        static void BuildLevelCompletePanel(Transform root, LevelUI ui)
        {
            var group = Node(root, "PainelFaseConcluida");
            Stretch(group);
            Dimmer(group, new Color(0.02f, 0.08f, 0.08f, 0.6f));
            var panel = Panel(group, "Painel", new Vector2(620, 480));
            var title = Label(panel.transform, "Titulo", "FASE CONCLUÍDA!", 54, Gold);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(580, 66));
            var stats = Label(panel.transform, "Estatisticas", "", 28, Cream);
            Place(stats.rectTransform, Center, Center, new Vector2(0, 50), new Vector2(560, 160));
            var list = VerticalGroup(panel.transform, "Botoes", new Vector2(0, -140), new Vector2(380, 140), 12);
            var next = Button(list, "ProximaFase", "PRÓXIMA FASE", new Vector2(360, 58));
            var menu = Button(list, "Menu", "MENU PRINCIPAL", new Vector2(360, 58));
            LinkVertical(next, menu);

            EditorUtil.Set(ui, "levelCompletePanel", group.gameObject);
            EditorUtil.Set(ui, "levelCompleteTitle", title);
            EditorUtil.Set(ui, "levelCompleteStats", stats);
            EditorUtil.Set(ui, "nextLevelButton", next);
            EditorUtil.Set(ui, "levelCompleteMenuButton", menu);
        }

        // ================================================================== MENU PRINCIPAL

        public static void BuildMainMenuUI(GameObject controllerHost)
        {
            var canvas = CreateCanvas("MenuCanvas", 5);
            var root = canvas.transform;
            var controller = controllerHost.AddComponent<MainMenuController>();

            var title = Label(root, "Titulo", "ECOS DA FLORESTA", 78, Gold);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1100, 96));
            var subtitle = Label(root, "Subtitulo", "A Jornada de Lumo", 36, Mint);
            Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -134), new Vector2(800, 48));

            // Painel principal.
            var main = Node(root, "PainelPrincipal");
            Stretch(main);
            var list = VerticalGroup(main, "Botoes", new Vector2(-250, -70), new Vector2(380, 440), 12);
            var newGame = Button(list, "NovoJogo", "NOVO JOGO", new Vector2(360, 58), 28);
            var cont = Button(list, "Continuar", "CONTINUAR", new Vector2(360, 58), 28);
            var levels = Button(list, "SelecionarFase", "SELECIONAR FASE", new Vector2(360, 58), 28);
            var how = Button(list, "ComoJogar", "COMO JOGAR", new Vector2(360, 58), 28);
            var sound = Button(list, "Som", "SOM: LIGADO", new Vector2(360, 58), 28);
            var quit = Button(list, "Sair", "SAIR", new Vector2(360, 58), 28);
            LinkVertical(newGame, cont, levels, how, sound, quit);

            var best = Label(root, "Recorde", "RECORDE: 000000", 24, Gold, TextAnchor.LowerRight);
            Place(best.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24, 18), new Vector2(400, 32));
            var credit = Label(root, "Creditos", "Projeto acadêmico de Game Development  •  Unity 6  •  Arte e áudio originais", 18, Cream, TextAnchor.LowerLeft);
            Place(credit.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24, 18), new Vector2(760, 28));

            // Seleção de fases.
            var levelPanelGroup = Node(root, "PainelFases");
            Stretch(levelPanelGroup);
            Dimmer(levelPanelGroup, new Color(0.03f, 0.02f, 0.08f, 0.55f));
            var levelPanel = Panel(levelPanelGroup, "Painel", new Vector2(720, 560));
            var lt = Label(levelPanel.transform, "Titulo", "SELECIONAR FASE", 46, Gold);
            Place(lt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(680, 58));
            var levelList = VerticalGroup(levelPanel.transform, "Fases", new Vector2(0, -20), new Vector2(640, 360), 16);
            var levelButtons = new Button[GameConstants.LevelCount];
            for (int i = 0; i < levelButtons.Length; i++)
                levelButtons[i] = Button(levelList, "Fase" + (i + 1), "FASE " + (i + 1), new Vector2(620, 92), 28);
            var levelBack = Button(levelList, "Voltar", "VOLTAR", new Vector2(260, 54), 24);
            var allLevel = new Button[levelButtons.Length + 1];
            levelButtons.CopyTo(allLevel, 0);
            allLevel[levelButtons.Length] = levelBack;
            LinkVertical(allLevel);

            // Como jogar.
            var howGroup = Node(root, "PainelComoJogar");
            Stretch(howGroup);
            Dimmer(howGroup, new Color(0.03f, 0.02f, 0.08f, 0.6f));
            var howPanel = Panel(howGroup, "Painel", new Vector2(1060, 620));
            var ht = Label(howPanel.transform, "Titulo", "COMO JOGAR", 46, Gold);
            Place(ht.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(1000, 58));
            var controls = Label(howPanel.transform, "Controles",
                "<color=#FFE06D>CONTROLES</color>\n" +
                "Andar: A / D  ou  ← / →\n" +
                "Correr: segure SHIFT (ou X)\n" +
                "Pular: ESPAÇO (ou Z)\n" +
                "Escalar: W / S  ou  ↑ / ↓\n" +
                "Descer da plataforma: ↓ + ESPAÇO\n" +
                "Pausar: ESC ou P   •   Som: M\n" +
                "Gamepad: analógico, A pula, X corre",
                22, Cream, TextAnchor.UpperLeft);
            Place(controls.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-490, -104), new Vector2(470, 400));
            var story = Label(howPanel.transform, "Historia",
                "<color=#FFE06D>A HISTÓRIA</color>\n" +
                "A energia da Floresta Encantada está desaparecendo e criaturas corrompidas dominam o lugar. " +
                "Ajude Lumo, o pequeno guardião, a recuperar os cristais de energia e restaurar o Cristal Primordial.\n\n" +
                "<color=#FFE06D>OBJETIVO</color>\n" +
                "Colete cristais (+100), pise nos inimigos (+150) e chegue ao portal. " +
                "Você tem 3 vidas por fase; os totens são checkpoints. O Espinhento não pode ser pisado!",
                21, Cream, TextAnchor.UpperLeft);
            Place(story.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(10, -104), new Vector2(490, 420));
            var howBack = Button(howPanel.transform, "Voltar", "VOLTAR", new Vector2(260, 54), 24);
            Place((RectTransform)howBack.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(260, 54));

            EditorUtil.Set(controller, "mainPanel", main.gameObject);
            EditorUtil.Set(controller, "levelSelectPanel", levelPanelGroup.gameObject);
            EditorUtil.Set(controller, "howToPlayPanel", howGroup.gameObject);
            EditorUtil.Set(controller, "newGameButton", newGame);
            EditorUtil.Set(controller, "continueButton", cont);
            EditorUtil.Set(controller, "levelSelectButton", levels);
            EditorUtil.Set(controller, "howToPlayButton", how);
            EditorUtil.Set(controller, "soundButton", sound);
            EditorUtil.Set(controller, "quitButton", quit);
            EditorUtil.Set(controller, "bestScoreText", best);
            EditorUtil.SetArray(controller, "levelButtons", levelButtons);
            EditorUtil.Set(controller, "levelSelectBackButton", levelBack);
            EditorUtil.Set(controller, "howToPlayBackButton", howBack);
        }

        // ================================================================== VITÓRIA

        public static void BuildVictoryUI(GameObject controllerHost)
        {
            var canvas = CreateCanvas("VitoriaCanvas", 5);
            var root = canvas.transform;
            var controller = controllerHost.AddComponent<VictoryController>();

            var title = Label(root, "Titulo", "PARABÉNS!", 84, Gold);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(1000, 100));
            var subtitle = Label(root, "Subtitulo", "Lumo restaurou o Cristal Primordial e a floresta voltou a brilhar!", 30, Mint);
            Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -136), new Vector2(1100, 44));

            var panel = Panel(root, "Painel", new Vector2(560, 400));
            Place(panel.rectTransform, Center, Center, new Vector2(-260, -90), new Vector2(560, 400));
            var stats = Label(panel.transform, "Estatisticas", "", 28, Cream);
            Place(stats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(500, 150));
            var list = VerticalGroup(panel.transform, "Botoes", new Vector2(0, -90), new Vector2(380, 200), 12);
            var again = Button(list, "JogarNovamente", "JOGAR NOVAMENTE", new Vector2(360, 56));
            var menu = Button(list, "Menu", "MENU PRINCIPAL", new Vector2(360, 56));
            var quit = Button(list, "Sair", "SAIR", new Vector2(360, 56));
            LinkVertical(again, menu, quit);

            EditorUtil.Set(controller, "statsText", stats);
            EditorUtil.Set(controller, "playAgainButton", again);
            EditorUtil.Set(controller, "menuButton", menu);
            EditorUtil.Set(controller, "quitButton", quit);
        }
    }
}

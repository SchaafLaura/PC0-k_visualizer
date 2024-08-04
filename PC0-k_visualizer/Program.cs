using SadConsole.Configuration;

Settings.WindowTitle = "My SadConsole Game";

Builder gameStartup = new Builder()
    .SetScreenSize(GameSettings.GAME_WIDTH, GameSettings.GAME_HEIGHT)
    .SetStartingScreen<PC0_k_visualizer.Scenes.RootScreen>()
    .IsStartingScreenFocused(true)
    .ConfigureFonts("Cheepicus12.font");

Game.Create(gameStartup);
Game.Instance.Run();
Game.Instance.Dispose();
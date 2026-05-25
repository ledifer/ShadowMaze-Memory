using System;
using System.Drawing;
using System.Windows.Forms;
using ShadowMaze.Model;
using ShadowMaze.Controller;

namespace ShadowMaze.View
{
    public partial class MainForm : Form
    {
        private GameModel model;
        private GameController controller;
        private MazeView mazeView;
        private PictureBox canvas;

        // музыка
        private System.Media.SoundPlayer backgroundMusic;

        public MainForm()
        {
            InitializeComponent();

            this.WindowState = FormWindowState.Maximized;
            this.Text = "Лабиринт Теней: Память";
            this.MinimumSize = new Size(400, 400);

            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20)
            };
            this.Controls.Add(canvas);

            // безопасная загрузка музыки
            try
            {
                backgroundMusic = new System.Media.SoundPlayer(Properties.Resources.background);
                backgroundMusic.PlayLooping();
            }
            catch
            {

            }

            this.FormClosing += (s, e) => backgroundMusic?.Dispose();

            mazeView = new MazeView(null, canvas);

            // главное мменю
            mazeView.IsMainMenu = true;
            mazeView.MainMenuStartRequested += StartGame;
            mazeView.MainMenuExitRequested += () =>
            {
                backgroundMusic?.Stop();
                backgroundMusic?.Dispose();
                Application.Exit();
            };
            mazeView.MainMenuReturnRequested += ShowMainMenu;

            canvas.Paint += mazeView.OnPaint;
            canvas.MouseClick += (s, e) => mazeView.HandleMouseClick(e);
            canvas.MouseMove += (s, e) => mazeView.HandleMouseMove(e);
            canvas.MouseUp += (s, e) => mazeView.HandleMouseUp(e);
            canvas.Invalidate();

            // кнопочки
            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;
        }

        private void ShowMainMenu()
        {
            model?.StopEnemyTimer();
            if (mazeView != null)
            {
                mazeView.IsMainMenu = true;
                mazeView.IsPlaying = false;
                mazeView.ShowVictory = false;
                mazeView.ShowDefeat = false;
                mazeView.IsMenuVisible = false;
                mazeView.IsPaused = false;
            }
            if (model != null)
            {
                model.PlayerMoved -= mazeView.OnModelChanged;
                model.MemoryChanged -= mazeView.OnModelChanged;
                model.GameWon -= OnGameWon;
                model.GameLost -= OnGameLost;
                model.EffectsUpdated -= () => canvas.Invalidate();
            }
            model = null;
            controller = null;
            canvas.Invalidate();
        }

        private void StartGame()
        {
            mazeView.IsMainMenu = false;
            mazeView.IsPlaying = true;
            mazeView.ShowVictory = false;
            mazeView.ShowDefeat = false;
            mazeView.IsMenuVisible = false;
            mazeView.IsPaused = false;

            InitializeGame();
        }

        private void InitializeGame()
        {
            model?.StopEnemyTimer();

            if (model != null)
            {
                model.PlayerMoved -= mazeView.OnModelChanged;
                model.MemoryChanged -= mazeView.OnModelChanged;
                model.GameWon -= OnGameWon;
                model.GameLost -= OnGameLost;
                model.EffectsUpdated -= () => canvas.Invalidate();
            }

            model = new GameModel(31, 31, 25);
            controller = new GameController(model);
            mazeView.SetModel(model);

            model.PlayerMoved += mazeView.OnModelChanged;
            model.MemoryChanged += mazeView.OnModelChanged;
            model.GameWon += OnGameWon;
            model.GameLost += OnGameLost;
            model.EffectsUpdated += () => canvas.Invalidate();

            mazeView.RestartRequested += StartGame;
            mazeView.ExitRequested += () => Application.Exit();

            canvas.Invalidate();
        }

        private void OnGameWon()
        {
            mazeView.ShowVictory = true;
            mazeView.IsPlaying = false;
            canvas.Invalidate();
        }

        private void OnGameLost()
        {
            mazeView.ShowDefeat = true;
            mazeView.IsPlaying = false;
            canvas.Invalidate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (mazeView.IsMainMenu || mazeView.ShowVictory || mazeView.ShowDefeat)
                return;

            if (e.KeyCode == Keys.Escape)
            {
                if (mazeView.IsMenuVisible)
                {
                    mazeView.IsMenuVisible = false;
                    mazeView.IsPaused = false;
                    model?.Resume();
                }
                else
                {
                    mazeView.IsMenuVisible = true;
                    mazeView.IsPaused = true;
                    model?.Pause();
                }
                canvas.Invalidate();
                e.Handled = true;
                return;
            }

            if (mazeView.IsPaused || (model != null && model.IsFinished))
                return;

            Direction? dir = null;
            switch (e.KeyCode)
            {
                case Keys.W: case Keys.Up: dir = Direction.Up; break;
                case Keys.S: case Keys.Down: dir = Direction.Down; break;
                case Keys.A: case Keys.Left: dir = Direction.Left; break;
                case Keys.D: case Keys.Right: dir = Direction.Right; break;
            }
            if (dir.HasValue)
            {
                controller?.HandleInput(dir.Value);
                e.Handled = true;
            }
        }
    }
}
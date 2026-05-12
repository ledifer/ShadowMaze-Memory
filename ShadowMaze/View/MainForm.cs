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

        public MainForm()
        {
            InitializeComponent();

            this.Text = "Лабиринт Теней: Память";
            this.MinimumSize = new Size(400, 400);

            // панель инструментов сверху
            Panel toolPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            this.Controls.Add(toolPanel);

            // надпись "Радиус:"
            Label lblRadius = new Label
            {
                Text = "Радиус:",
                ForeColor = Color.White,
                Location = new Point(5, 8),
                AutoSize = true
            };
            toolPanel.Controls.Add(lblRadius);

            // поле выбора радиуса обзора
            NumericUpDown numRadius = new NumericUpDown
            {
                Location = new Point(55, 5),
                Width = 45,
                Minimum = 1,
                Maximum = 10,
                Value = 3
            };
            toolPanel.Controls.Add(numRadius);

            // кнопка пересоздания лабиринта
            Button btnRestart = new Button
            {
                Text = "Новый лабиринт",
                Location = new Point(110, 4),
                Width = 110,
                Height = 25
            };
            toolPanel.Controls.Add(btnRestart);

            // галочка для отладки — показывает весь лабиринт
            CheckBox chkFullVision = new CheckBox
            {
                Text = "Полная видимость",
                ForeColor = Color.White,
                Location = new Point(230, 7),
                AutoSize = true
            };
            toolPanel.Controls.Add(chkFullVision);

            // временная кнопка для теста движения (потом убрать)
            Button btnTestRight = new Button
            {
                Text = "Тест вправо",
                Location = new Point(355, 4),
                Width = 90,
                Height = 25
            };
            toolPanel.Controls.Add(btnTestRight);

            // холст для игры
            canvas = new PictureBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20) // чуть светлее чёрного, чтобы видеть край лабиринта
            };
            this.Controls.Add(canvas);

            // запускаем первую игру
            InitializeGame();

            // связываем элементы панели с моделью
            numRadius.ValueChanged += (s, e) =>
            {
                if (model != null) model.Player.VisionRadius = (int)numRadius.Value;
                canvas.Invalidate();
            };

            btnRestart.Click += (s, e) => InitializeGame();

            chkFullVision.CheckedChanged += (s, e) =>
            {
                if (model != null) model.FullVisibility = chkFullVision.Checked;
                canvas.Invalidate();
            };

            btnTestRight.Click += (s, e) =>
            {
                controller?.HandleInput(Direction.Right);
            };

            // при изменении размеров окна перерисовываем
            canvas.Resize += (s, e) => canvas.Invalidate();
        }

        // надёжный перехват клавиш: работает, даже если фокус на панели или кнопках
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (model.IsFinished) return false;
            Direction? dir = null;
            switch (keyData)
            {
                case Keys.W: case Keys.Up: dir = Direction.Up; break;
                case Keys.S: case Keys.Down: dir = Direction.Down; break;
                case Keys.A: case Keys.Left: dir = Direction.Left; break;
                case Keys.D: case Keys.Right: dir = Direction.Right; break;
            }
            if (dir.HasValue)
            {
                controller?.HandleInput(dir.Value);
                return true; // клавиша обработана
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // создаёт новую игру (при старте и по кнопке "Новый лабиринт")
        private void InitializeGame()
        {

            // останавливаем старый таймер врагов, если был
            model?.StopEnemyTimer();

            // отписываем старый MazeView от событий модели
            if (mazeView != null)
            {
                mazeView.ShowVictory = false;
                mazeView.ShowDefeat = false;
                mazeView.StopAnimation();
                model.PlayerMoved -= mazeView.OnModelChanged;
                model.MemoryChanged -= mazeView.OnModelChanged;
                model.GameWon -= OnGameWon;   
                model.GameLost -= OnGameLost; 
                canvas.Paint -= mazeView.OnPaint;
            }

            // создаём новые модель, контроллер и представление
            model = new GameModel(31, 31, 27);
            controller = new GameController(model);
            mazeView = new MazeView(model, canvas);
            mazeView.RestartRequested += () => InitializeGame();
            mazeView.ExitRequested += () => Application.Exit();

            // подписываемся на события модели
            model.PlayerMoved += mazeView.OnModelChanged;
            model.MemoryChanged += mazeView.OnModelChanged;
            model.GameWon += OnGameWon;
            model.GameLost += OnGameLost;           // обрабатываем проигрыш

            canvas.Paint += mazeView.OnPaint;
            canvas.Invalidate();
        }
        private void OnGameWon()
        {
            mazeView.StopAnimation();
            mazeView.ShowVictory = true;
            canvas.Invalidate();
        }

        private void OnGameLost()
        {
            mazeView.StopAnimation();
            mazeView.ShowDefeat = true;
            canvas.Invalidate();
        }
    }
}
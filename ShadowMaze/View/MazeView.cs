using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ShadowMaze.Model;

namespace ShadowMaze.View
{
    public class MazeView
    {
        private GameModel model;
        private PictureBox canvas;

        private int cellSize;
        private int offsetX;
        private int offsetY;

        // текстуры
        private Image wallImage;
        private Image floorImage;
        private Image exitImage;
        private Image playerImage;

        // мерцающие огоньки
        private List<PointF> flickerLights;
        private Random random = new Random();
        private System.Windows.Forms.Timer animationTimer;

        // финальное сообщение
        public bool ShowVictory { get; set; } = false;
        public bool ShowDefeat { get; set; } = false;
        private Rectangle btnRestartRect;
        private Rectangle btnExitRect;
        public event Action RestartRequested;
        public event Action ExitRequested;

        public MazeView(GameModel model, PictureBox canvas)
        {
            this.model = model;
            this.canvas = canvas;

            UpdateCellSize();
            InitFlickerLights();

            canvas.Resize += (s, e) =>
            {
                UpdateCellSize();
                InitFlickerLights();
                UpdateButtonRects();
                canvas.Invalidate();
            };

            // таймер анимации огоньков (25 кадров/с)
            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 40;
            animationTimer.Tick += (s, e) => canvas.Invalidate();
            animationTimer.Start();

            // обновление при движении врагов
            model.EnemiesUpdated += () => canvas.Invalidate();

            // клик по кнопкам финального сообщения
            canvas.MouseClick += (s, e) => HandleMouseClick(e.Location);
            UpdateButtonRects();
        }

        // вызывается при любом изменении модели (игрок пошёл, радиус обзора и т.д.)
        public void OnModelChanged()
        {
            UpdateCellSize();
            canvas.Invalidate();
        }

        public void StopAnimation()
        {
            animationTimer?.Stop();
            animationTimer?.Dispose();
        }

        // основная отрисовка
        public void OnPaint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black); // неисследованная внутренность

            Maze maze = model.Maze;
            Player player = model.Player;

            // 1. клетки лабиринта
            for (int x = 0; x < maze.Width; x++)
            {
                for (int y = 0; y < maze.Height; y++)
                {
                    Cell? cell = maze.GetCell(x, y);
                    if (cell == null) continue;

                    Rectangle rect = new Rectangle(
                        x * cellSize + offsetX,
                        y * cellSize + offsetY,
                        cellSize, cellSize);

                    bool inSight = model.IsInSight(x, y);
                    if (model.FullVisibility) inSight = true; // отладка
                    bool remembered = model.Memory.IsRemembered(x, y);

                    if (inSight)
                    {
                        // прямая видимость
                        if (cell.IsExit)
                        {
                            if (exitImage != null) g.DrawImage(exitImage, rect);
                            else g.FillRectangle(Brushes.Green, rect);
                        }
                        else if (cell.IsWall)
                        {
                            if (wallImage != null) g.DrawImage(wallImage, rect);
                            else g.FillRectangle(Brushes.DarkBlue, rect);
                        }
                        else
                        {
                            if (floorImage != null) g.DrawImage(floorImage, rect);
                            else g.FillRectangle(Brushes.DimGray, rect);
                        }
                    }
                    else if (remembered)
                    {
                        // запомненные, но не видимые напрямую
                        bool enemyOnCell = false;
                        if (!cell.IsWall)
                        {
                            foreach (var enemy in model.Enemies)
                            {
                                if (enemy.X == x && enemy.Y == y)
                                {
                                    enemyOnCell = true;
                                    break;
                                }
                            }
                        }

                        if (enemyOnCell)
                        {
                            // мерцание красноватым, если на клетке враг
                            float flicker = (float)(Math.Sin(Environment.TickCount * 0.01 + x * 0.5 + y * 0.5) * 0.5 + 0.5);
                            int alpha = (int)(30 + flicker * 40);
                            using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, 200, 50, 50)))
                                g.FillRectangle(brush, rect);
                        }
                        else
                        {
                            // обычная тусклая память
                            if (cell.IsExit)
                                g.FillRectangle(Brushes.DarkGreen, rect);
                            else if (cell.IsWall)
                                g.FillRectangle(Brushes.MidnightBlue, rect);
                            else
                                g.FillRectangle(Brushes.DarkSlateGray, rect);
                        }
                    }
                }
            }

            // 2. враги (видны только в прямой видимости)
            foreach (var enemy in model.Enemies)
            {
                if (model.IsInSight(enemy.X, enemy.Y))
                {
                    Rectangle enemyRect = new Rectangle(
                        enemy.X * cellSize + offsetX,
                        enemy.Y * cellSize + offsetY,
                        cellSize, cellSize);
                    g.FillEllipse(Brushes.Red, enemyRect);
                }
            }

            // 3. игрок
            Rectangle playerRect = new Rectangle(
                player.X * cellSize + offsetX,
                player.Y * cellSize + offsetY,
                cellSize, cellSize);
            if (playerImage != null)
                g.DrawImage(playerImage, playerRect);
            else
                g.FillRectangle(Brushes.Yellow, playerRect);

            // 4. подсветка старта (если не виден)
            if (!model.IsInSight(1, 1))
            {
                Rectangle startRect = new Rectangle(1 * cellSize + offsetX, 1 * cellSize + offsetY, cellSize, cellSize);
                int cx = startRect.X + startRect.Width / 2;
                int cy = startRect.Y + startRect.Height / 2;
                int s = cellSize / 4;
                g.FillRectangle(Brushes.Red, cx - s / 2, cy - s / 2, s, s);
            }

            // 5. подсветка выхода (если не виден)
            int exitX = maze.Width - 2;
            int exitY = maze.Height - 2;
            if (!model.IsInSight(exitX, exitY))
            {
                Rectangle exitRect = new Rectangle(exitX * cellSize + offsetX, exitY * cellSize + offsetY, cellSize, cellSize);
                int cx = exitRect.X + exitRect.Width / 2;
                int cy = exitRect.Y + exitRect.Height / 2;
                int s = cellSize / 4;
                g.FillRectangle(Brushes.LimeGreen, cx - s / 2, cy - s / 2, s, s);
            }

            // 6. рамка лабиринта
            int mazeW = maze.Width * cellSize;
            int mazeH = maze.Height * cellSize;
            g.DrawRectangle(Pens.DimGray, offsetX, offsetY, mazeW, mazeH);

            // 7. мерцающие огоньки вокруг
            foreach (var light in flickerLights)
            {
                float flicker = (float)(Math.Sin(Environment.TickCount * 0.003 + light.X * 0.1 + light.Y * 0.1) * 0.5 + 0.5);
                int alpha = (int)(40 + flicker * 80);
                int radius = (int)(2 + flicker * 3);
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 100)))
                {
                    g.FillEllipse(brush, light.X - radius, light.Y - radius, radius * 2, radius * 2);
                }
            }

            // 8. финальное сообщение (победа/поражение)
            if (ShowVictory || ShowDefeat)
            {
                UpdateButtonRects();
                using (SolidBrush overlay = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                    g.FillRectangle(overlay, canvas.ClientRectangle);

                string message = ShowVictory ? "Вы победили!" : "Вас поймали!";
                using (Font font = new Font("Segoe UI", 18, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(message, font);
                    g.DrawString(message, font, textBrush,
                                 canvas.Width / 2 - textSize.Width / 2,
                                 canvas.Height / 2 - 60);
                }

                DrawButton(g, btnRestartRect, "Заново");
                DrawButton(g, btnExitRect, "Выход");
            }
        }

        // пересчёт размера клетки и центрирования
        private void UpdateCellSize()
        {
            if (canvas.Width == 0 || canvas.Height == 0) return;
            int maxCellW = canvas.Width / model.Maze.Width;
            int maxCellH = canvas.Height / model.Maze.Height;
            cellSize = Math.Min(maxCellW, maxCellH);
            if (cellSize < 5) cellSize = 5;

            int mazePixelW = cellSize * model.Maze.Width;
            int mazePixelH = cellSize * model.Maze.Height;
            offsetX = (canvas.Width - mazePixelW) / 2;
            offsetY = (canvas.Height - mazePixelH) / 2;
        }

        // создание огоньков вокруг лабиринта (с защитой от ошибок диапазона)
        private void InitFlickerLights()
        {
            flickerLights = new List<PointF>();
            int margin = 20;
            int countPerSide = 30;

            int mazeRight = offsetX + model.Maze.Width * cellSize;
            int mazeBottom = offsetY + model.Maze.Height * cellSize;

            // слева
            if (offsetX > margin + 5)
                for (int i = 0; i < countPerSide; i++)
                    flickerLights.Add(new PointF(
                        random.Next(5, offsetX - margin),
                        random.Next(5, canvas.Height - 5)));

            // справа
            if (canvas.Width - mazeRight > margin + 5)
                for (int i = 0; i < countPerSide; i++)
                    flickerLights.Add(new PointF(
                        random.Next(mazeRight + margin, canvas.Width - 5),
                        random.Next(5, canvas.Height - 5)));

            // сверху
            if (offsetY > margin + 5)
                for (int i = 0; i < countPerSide; i++)
                    flickerLights.Add(new PointF(
                        random.Next(offsetX, mazeRight),
                        random.Next(5, offsetY - margin)));

            // снизу
            if (canvas.Height - mazeBottom > margin + 5)
                for (int i = 0; i < countPerSide; i++)
                    flickerLights.Add(new PointF(
                        random.Next(offsetX, mazeRight),
                        random.Next(mazeBottom + margin, canvas.Height - 5)));
        }

        // кнопки финального сообщения
        private void UpdateButtonRects()
        {
            int w = 120, h = 35;
            int centerX = canvas.Width / 2;
            int centerY = canvas.Height / 2 + 20;
            btnRestartRect = new Rectangle(centerX - w - 10, centerY, w, h);
            btnExitRect = new Rectangle(centerX + 10, centerY, w, h);
        }

        private void HandleMouseClick(Point location)
        {
            if (!ShowVictory && !ShowDefeat) return;
            if (btnRestartRect.Contains(location))
                RestartRequested?.Invoke();
            else if (btnExitRect.Contains(location))
                ExitRequested?.Invoke();
        }

        private void DrawButton(Graphics g, Rectangle rect, string text)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(200, 60, 60, 60)))
            using (Pen border = new Pen(Color.Gray))
            using (Font font = new Font("Segoe UI", 12))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(bg, rect);
                g.DrawRectangle(border, rect);
                SizeF ts = g.MeasureString(text, font);
                g.DrawString(text, font, textBrush,
                             rect.X + rect.Width / 2 - ts.Width / 2,
                             rect.Y + rect.Height / 2 - ts.Height / 2);
            }
        }
    }
}
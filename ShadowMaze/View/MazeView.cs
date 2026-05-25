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
        private Image patrolEnemyImage;
        private Image chaserEnemyImage;
        private Image itemMemoryImage;
        private Image itemVisionImage;
        private Image itemSlowImage;

        private Image wallImageScaled;
        private Image floorImageScaled;
        private Image floorMemoryImage;
        private Image floorMemoryScaled;
        private Image exitImageScaled;
        private Image playerImageScaled;
        private Image patrolEnemyImageScaled;
        private Image chaserEnemyImageScaled;
        private Image itemMemoryImageScaled;
        private Image itemVisionImageScaled;
        private Image itemSlowImageScaled;


        // состояния
        public bool IsMainMenu { get; set; } = false;
        public bool IsPlaying { get; set; } = false;
        public bool ShowVictory { get; set; } = false;
        public bool ShowDefeat { get; set; } = false;
        public bool IsMenuVisible { get; set; } = false;
        public bool IsPaused { get; set; } = false;

        // финальные кнопочки
        private Rectangle btnRestartRect;
        private Rectangle btnExitRect;
        private bool btnRestartHovered;
        private bool btnExitHovered;
        public event Action RestartRequested;
        public event Action ExitRequested;

        // главное меню
        public event Action MainMenuStartRequested;
        public event Action MainMenuExitRequested;
        public event Action MainMenuReturnRequested;

        private Rectangle btnStartRect;
        private Rectangle btnExitMainMenuRect;
        private bool btnStartHovered;
        private bool btnExitMainMenuHovered;

        // пауза
        private Rectangle menuCloseRect;
        private Rectangle menuFullVisionRect;
        private Rectangle menuRestartRect;
        private Rectangle menuMainMenuRect;
        private bool menuFullVisionHovered;
        private bool menuRestartHovered;
        private bool menuCloseHovered;
        private bool menuMainMenuHovered;

        
        public MazeView(GameModel model, PictureBox canvas)
        {
            this.model = model;
            this.canvas = canvas;

            wallImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("wall"));
            floorImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("floor"));
            exitImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("exit"));
            playerImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("player"));
            patrolEnemyImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("enemy_patrol"));
            chaserEnemyImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("enemy_chaser"));
            itemMemoryImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("item_memory"));
            itemVisionImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("item_vision"));
            itemSlowImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("item_slow"));
            floorMemoryImage = LoadImageSafe(Properties.Resources.ResourceManager.GetObject("floor_memory"));

            canvas.Resize += (s, e) =>
            {
                if (model != null)
                {
                    UpdateCellSize();
                    ScaleImages();
                }
                canvas.Invalidate();
            };

        }


        public void SetModel(GameModel newModel)
        {
            this.model = newModel;
            if (model != null)
            {
                model.EnemiesUpdated += () => canvas.Invalidate();
                model.ItemsUpdated += () => canvas.Invalidate();

                UpdateCellSize();
                ScaleImages();
            }
            canvas.Invalidate();
        }

        public void OnModelChanged()
        {
            UpdateCellSize();
            canvas.Invalidate();
        }

        // безопасная загрузка текстур 
        private Image LoadImageSafe(object resourceObject)
        {
            try
            {
                return resourceObject as Image;
            }
            catch
            {
                return null;
            }
        }

        // рисуем
        public void OnPaint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            if (IsMainMenu)
            {
                DrawMainMenu(g);
                return;
            }

            if (IsPlaying || ShowVictory || ShowDefeat)
            {
                DrawGame(g);
            }

            if (IsMenuVisible && IsPlaying)
            {
                DrawSettingsMenu(g);
            }

            if (ShowVictory || ShowDefeat)
            {
                DrawEndGameScreen(g);
            }
        }

        private void DrawGame(Graphics g)
        {
            if (model == null) return;
            Maze maze = model.Maze;
            Player player = model.Player;

            // клетки
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
                    if (model.FullVisibility) inSight = true;
                    bool remembered = model.Memory.IsRemembered(x, y);

                    if (inSight)
                    {
                        if (cell.IsExit)
                        {
                            if (exitImageScaled != null) g.DrawImage(exitImageScaled, rect);
                            else g.FillRectangle(Brushes.Green, rect);
                        }
                        else if (cell.IsWall)
                        {
                            if (wallImageScaled != null) g.DrawImage(wallImageScaled, rect);
                            else g.FillRectangle(Brushes.DarkBlue, rect);
                        }
                        else
                        {
                            if (floorImageScaled != null) g.DrawImage(floorImageScaled, rect);
                            else g.FillRectangle(Brushes.DimGray, rect);
                        }
                    }
                    else if (remembered)
                    {
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
                            float flicker = (float)(Math.Sin(Environment.TickCount * 0.01 + x * 0.5 + y * 0.5) * 0.5 + 0.5);
                            int alpha = (int)(30 + flicker * 40);
                            using (SolidBrush brush = new SolidBrush(Color.FromArgb(alpha, 200, 50, 50)))
                                g.FillRectangle(brush, rect);
                        }
                        else
                        {
                            if (cell.IsExit)
                                g.FillRectangle(Brushes.DarkGreen, rect);
                            else if (cell.IsWall)
                                g.FillRectangle(Brushes.MidnightBlue, rect);
                            else 
                            {
                                if (floorMemoryScaled != null) g.DrawImage(floorMemoryScaled, rect);
                                else g.FillRectangle(Brushes.DarkSlateGray, rect);
                            }
                        }
                    }
                }
            }

            // враги
            foreach (var enemy in model.Enemies)
            {
                if (model.IsInSight(enemy.X, enemy.Y) || model.FullVisibility)
                {
                    Rectangle enemyRect = new Rectangle(
                        enemy.X * cellSize + offsetX,
                        enemy.Y * cellSize + offsetY,
                        cellSize, cellSize);

                    if (enemy.IsPatrol)
                    {
                        if (patrolEnemyImageScaled != null) g.DrawImage(patrolEnemyImageScaled, enemyRect);
                        else if (patrolEnemyImage != null) g.DrawImage(patrolEnemyImage, enemyRect);
                        else g.FillEllipse(Brushes.Orange, enemyRect);
                    }
                    else
                    {
                        if (chaserEnemyImageScaled != null) g.DrawImage(chaserEnemyImageScaled, enemyRect);
                        else if (chaserEnemyImage != null) g.DrawImage(chaserEnemyImage, enemyRect);
                        else g.FillEllipse(Brushes.Red, enemyRect);
                    }
                }
            }

            // предметы
            foreach (var item in model.Items)
            {
                if (model.IsInSight(item.X, item.Y) || model.FullVisibility)
                {
                    Rectangle itemRect = new Rectangle(
                        item.X * cellSize + offsetX,
                        item.Y * cellSize + offsetY,
                        cellSize, cellSize);

                    switch (item.Type)
                    {
                        case ItemType.MemoryBoost:
                            if (itemMemoryImageScaled != null) g.DrawImage(itemMemoryImageScaled, itemRect);
                            else if (itemMemoryImage != null) g.DrawImage(itemMemoryImage, itemRect);
                            else g.FillEllipse(Brushes.Cyan, itemRect);
                            break;
                        case ItemType.VisionBoost:
                            if (itemVisionImageScaled != null) g.DrawImage(itemVisionImageScaled, itemRect);
                            else if (itemVisionImage != null) g.DrawImage(itemVisionImage, itemRect);
                            else g.FillEllipse(Brushes.Yellow, itemRect);
                            break;
                        case ItemType.SlowCrystal:
                            if (itemSlowImageScaled != null) g.DrawImage(itemSlowImageScaled, itemRect);
                            else if (itemSlowImage != null) g.DrawImage(itemSlowImage, itemRect);
                            else g.FillEllipse(Brushes.Magenta, itemRect);
                            break;
                    }
                }
            }

            // сам игрок
            Rectangle playerRect = new Rectangle(
                player.X * cellSize + offsetX,
                player.Y * cellSize + offsetY,
                cellSize, cellSize);
            if (playerImageScaled != null) g.DrawImage(playerImageScaled, playerRect);
            else if (playerImage != null) g.DrawImage(playerImage, playerRect);
            else g.FillRectangle(Brushes.Yellow, playerRect);

            // старт
            if (!model.IsInSight(1, 1))
            {
                Rectangle startRect = new Rectangle(1 * cellSize + offsetX, 1 * cellSize + offsetY, cellSize, cellSize);
                int cx = startRect.X + startRect.Width / 2;
                int cy = startRect.Y + startRect.Height / 2;
                int s = cellSize / 4;
                g.FillRectangle(Brushes.Red, cx - s / 2, cy - s / 2, s, s);
            }

            // рамка
            int mazeW = maze.Width * cellSize;
            int mazeH = maze.Height * cellSize;
            g.DrawRectangle(Pens.DimGray, offsetX, offsetY, mazeW, mazeH);


            // статус эффектов с рамкой и фоном
            if (model.ActiveEffects.Count > 0)
            {
                using (Font font = new Font("Segoe UI", 12, FontStyle.Bold))
                {
                    int lineHeight = 20;
                    int padding = 6;
                    int maxWidth = 0;
                    List<string> lines = new List<string>();

                    foreach (var effect in model.ActiveEffects)
                    {
                        string name = effect.Type switch
                        {
                            ItemType.MemoryBoost => "Увеличение Памяти",
                            ItemType.VisionBoost => "Увеличение Обзора",
                            ItemType.SlowCrystal => "Замедление Врагов",
                            _ => "?"
                        };
                        string line = $"{name}: {effect.RemainingTicks * 0.3:F1}с";
                        lines.Add(line);
                        SizeF lineSize = g.MeasureString(line, font);
                        if (lineSize.Width > maxWidth) maxWidth = (int)lineSize.Width;
                    }

                    int boxWidth = maxWidth + padding * 2;
                    int boxHeight = lines.Count * lineHeight + padding * 2;
                    Rectangle boxRect = new Rectangle(5, 5, boxWidth, boxHeight);

                    // фон и рамка
                    using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                        g.FillRectangle(bgBrush, boxRect);
                    g.DrawRectangle(Pens.Cyan, boxRect);

                    // текст поверх
                    using (SolidBrush textBrush = new SolidBrush(Color.Cyan))
                    {
                        int yOffset = boxRect.Y + padding;
                        foreach (string line in lines)
                        {
                            g.DrawString(line, font, textBrush, boxRect.X + padding, yOffset);
                            yOffset += lineHeight;
                        }
                    }
                }
            }
            // таймер 
            if (model.TimerStarted)
            {
                string timeText = model.GetElapsedString();
                using (Font font = new Font("Consolas", 14, FontStyle.Bold))
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    SizeF textSize = g.MeasureString(timeText, font);
                    float xPos = canvas.Width - textSize.Width - 15;
                    float yPos = 10;
                    // фон
                    RectangleF bgRect = new RectangleF(xPos - 4, yPos - 2, textSize.Width + 8, textSize.Height + 4);
                    g.FillRectangle(Brushes.Black, bgRect);
                    g.DrawRectangle(Pens.Gray, bgRect);
                    g.DrawString(timeText, font, brush, xPos, yPos);
                }
            }
        }

        private void DrawMainMenu(Graphics g)
        {
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(20, 20, 20)))
                g.FillRectangle(bg, canvas.ClientRectangle);

            using (Font titleFont = new Font("Segoe UI", 36, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.Cyan))
            {
                string title = "Лабиринт Теней: Память";
                SizeF size = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, titleBrush, canvas.Width / 2 - size.Width / 2, canvas.Height / 4);
            }

            // кнопка "Начать игру"
            btnStartRect = new Rectangle(canvas.Width / 2 - 100, canvas.Height / 2 - 30, 200, 40);
            Color startColor = btnStartHovered ? Color.LightBlue : Color.DarkSlateGray;
            using (SolidBrush brush = new SolidBrush(startColor))
            using (Pen pen = new Pen(Color.White))
            {
                g.FillRectangle(brush, btnStartRect);
                g.DrawRectangle(Pens.White, btnStartRect);
            }
            using (Font btnFont = new Font("Segoe UI", 12))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                string text = "Начать игру";
                SizeF ts = g.MeasureString(text, btnFont);
                g.DrawString(text, btnFont, textBrush,
                    btnStartRect.X + btnStartRect.Width / 2 - ts.Width / 2,
                    btnStartRect.Y + btnStartRect.Height / 2 - ts.Height / 2);
            }

            // кнопка "Выход"
            btnExitMainMenuRect = new Rectangle(canvas.Width / 2 - 100, canvas.Height / 2 + 30, 200, 40);
            Color exitColor = btnExitMainMenuHovered ? Color.LightBlue : Color.DarkSlateGray;
            using (SolidBrush brush = new SolidBrush(exitColor))
            using (Pen pen = new Pen(Color.White))
            {
                g.FillRectangle(brush, btnExitMainMenuRect);
                g.DrawRectangle(Pens.White, btnExitMainMenuRect);
            }
            using (Font btnFont = new Font("Segoe UI", 12))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                string text = "Выход";
                SizeF ts = g.MeasureString(text, btnFont);
                g.DrawString(text, btnFont, textBrush,
                    btnExitMainMenuRect.X + btnExitMainMenuRect.Width / 2 - ts.Width / 2,
                    btnExitMainMenuRect.Y + btnExitMainMenuRect.Height / 2 - ts.Height / 2);
            }
        }

        private void DrawSettingsMenu(Graphics g)
        {
            using (SolidBrush overlay = new SolidBrush(Color.FromArgb(150, 0, 0, 0)))
                g.FillRectangle(overlay, canvas.ClientRectangle);

            int menuW = 300, menuH = 240;
            int menuX = canvas.Width / 2 - menuW / 2;
            int menuY = canvas.Height / 2 - menuH / 2;
            Rectangle menuRect = new Rectangle(menuX, menuY, menuW, menuH);
            using (SolidBrush menuBg = new SolidBrush(Color.FromArgb(220, 50, 50, 50)))
            using (Pen menuBorder = new Pen(Color.Gray))
            {
                g.FillRectangle(menuBg, menuRect);
                g.DrawRectangle(menuBorder, menuRect);
            }

            using (Font titleFont = new Font("Segoe UI", 14, FontStyle.Bold))
            using (SolidBrush titleBrush = new SolidBrush(Color.White))
            {
                string title = "Настройки";
                SizeF ts = g.MeasureString(title, titleFont);
                g.DrawString(title, titleFont, titleBrush,
                    menuX + menuW / 2 - ts.Width / 2, menuY + 10);
            }

            // полная видимость
            menuFullVisionRect = new Rectangle(menuX + 30, menuY + 50, menuW - 60, 35);
            Color fvColor = menuFullVisionHovered ? Color.LightBlue : Color.DarkSlateGray;
            using (SolidBrush brush = new SolidBrush(fvColor))
            using (Pen pen = new Pen(Color.White))
            {
                g.FillRectangle(brush, menuFullVisionRect);
                g.DrawRectangle(Pens.White, menuFullVisionRect);
            }
            using (Font btnFont = new Font("Segoe UI", 10))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                string text = (model != null && model.FullVisibility) ? "Полная видимость: ВКЛ" : "Полная видимость: ВЫКЛ";
                SizeF ts = g.MeasureString(text, btnFont);
                g.DrawString(text, btnFont, textBrush,
                    menuFullVisionRect.X + menuFullVisionRect.Width / 2 - ts.Width / 2,
                    menuFullVisionRect.Y + menuFullVisionRect.Height / 2 - ts.Height / 2);
            }

            // новый лабиринт
            menuRestartRect = new Rectangle(menuX + 30, menuY + 95, menuW - 60, 35);
            Color resColor = menuRestartHovered ? Color.LightBlue : Color.DarkSlateGray;
            using (SolidBrush brush = new SolidBrush(resColor))
            using (Pen pen = new Pen(Color.White))
            {
                g.FillRectangle(brush, menuRestartRect);
                g.DrawRectangle(Pens.White, menuRestartRect);
            }
            using (Font btnFont = new Font("Segoe UI", 10))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                string text = "Новый лабиринт";
                SizeF ts = g.MeasureString(text, btnFont);
                g.DrawString(text, btnFont, textBrush,
                    menuRestartRect.X + menuRestartRect.Width / 2 - ts.Width / 2,
                    menuRestartRect.Y + menuRestartRect.Height / 2 - ts.Height / 2);
            }

            // выход в главное меню
            menuMainMenuRect = new Rectangle(menuX + 30, menuY + 140, menuW - 60, 35);
            Color mmColor = menuMainMenuHovered ? Color.LightBlue : Color.DarkSlateGray;
            using (SolidBrush brush = new SolidBrush(mmColor))
            using (Pen pen = new Pen(Color.White))
            {
                g.FillRectangle(brush, menuMainMenuRect);
                g.DrawRectangle(Pens.White, menuMainMenuRect);
            }
            using (Font btnFont = new Font("Segoe UI", 10))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                string text = "Выход в главное меню";
                SizeF ts = g.MeasureString(text, btnFont);
                g.DrawString(text, btnFont, textBrush,
                    menuMainMenuRect.X + menuMainMenuRect.Width / 2 - ts.Width / 2,
                    menuMainMenuRect.Y + menuMainMenuRect.Height / 2 - ts.Height / 2);
            }

            // закрыть
            menuCloseRect = new Rectangle(menuX + 30, menuY + 185, menuW - 60, 35);
            Color closeColor = menuCloseHovered ? Color.LightBlue : Color.DarkSlateGray;
            using (SolidBrush brush = new SolidBrush(closeColor))
            using (Pen pen = new Pen(Color.White))
            {
                g.FillRectangle(brush, menuCloseRect);
                g.DrawRectangle(Pens.White, menuCloseRect);
            }
            using (Font btnFont = new Font("Segoe UI", 10))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                string text = "Закрыть";
                SizeF ts = g.MeasureString(text, btnFont);
                g.DrawString(text, btnFont, textBrush,
                    menuCloseRect.X + menuCloseRect.Width / 2 - ts.Width / 2,
                    menuCloseRect.Y + menuCloseRect.Height / 2 - ts.Height / 2);
            }
        }

        private void DrawEndGameScreen(Graphics g)
        {
            UpdateButtonRects();
            using (SolidBrush overlay = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                g.FillRectangle(overlay, canvas.ClientRectangle);

            string message = ShowVictory ? "Вы победили!" : "Вас поймали!";
            using (Font font = new Font("Segoe UI", 18, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                SizeF ts = g.MeasureString(message, font);
                g.DrawString(message, font, textBrush,
                    canvas.Width / 2 - ts.Width / 2,
                    canvas.Height / 2 - 60);
            }
            // таймер время итог
            string timeText = $"Время: {model.GetElapsedString()}";
            using (Font timeFont = new Font("Segoe UI", 14, FontStyle.Bold))
            using (SolidBrush timeBrush = new SolidBrush(Color.Cyan))
            {
                SizeF timeSize = g.MeasureString(timeText, timeFont);
                g.DrawString(timeText, timeFont, timeBrush,
                    canvas.Width / 2 - timeSize.Width / 2,
                    canvas.Height / 2 - 30);
            }

            DrawButton(g, btnRestartRect, "Заново", btnRestartHovered);
            DrawButton(g, btnExitRect, "Выход в меню", btnExitHovered);
        }

        private void DrawButton(Graphics g, Rectangle rect, string text, bool hovered)
        {
            Color bgColor = hovered ? Color.FromArgb(200, 80, 80, 80) : Color.FromArgb(200, 60, 60, 60);
            Color borderColor = hovered ? Color.White : Color.Gray;
            using (SolidBrush bg = new SolidBrush(bgColor))
            using (Pen border = new Pen(borderColor))
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

        // обработка мыши
        public void HandleMouseClick(MouseEventArgs e)
        {
            if (IsMainMenu)
            {
                if (btnStartRect.Contains(e.Location))
                    MainMenuStartRequested?.Invoke();
                else if (btnExitMainMenuRect.Contains(e.Location))
                    MainMenuExitRequested?.Invoke();
                return;
            }

            if (IsMenuVisible)
            {
                if (menuCloseRect.Contains(e.Location))
                {
                    IsMenuVisible = false;
                    IsPaused = false;
                    model?.Resume();
                    canvas.Invalidate();
                    return;
                }
                if (menuFullVisionRect.Contains(e.Location))
                {
                    if (model != null) model.FullVisibility = !model.FullVisibility;
                    canvas.Invalidate();
                    return;
                }
                if (menuRestartRect.Contains(e.Location))
                {
                    IsMenuVisible = false;
                    IsPaused = false;
                    RestartRequested?.Invoke();
                    return;
                }
                if (menuMainMenuRect.Contains(e.Location))
                {
                    IsMenuVisible = false;
                    IsPaused = false;
                    MainMenuReturnRequested?.Invoke();
                    return;
                }
                IsMenuVisible = false;
                IsPaused = false;
                model?.Resume();
                canvas.Invalidate();
                return;
            }

            if (ShowVictory || ShowDefeat)
            {
                if (btnRestartRect.Contains(e.Location))
                {
                    RestartRequested?.Invoke();
                    return;
                }
                if (btnExitRect.Contains(e.Location))
                {
                    MainMenuReturnRequested?.Invoke();
                    return;
                }
            }
        }

        public void HandleMouseMove(MouseEventArgs e)
        {
            if (IsMainMenu)
            {
                btnStartHovered = btnStartRect.Contains(e.Location);
                btnExitMainMenuHovered = btnExitMainMenuRect.Contains(e.Location);
                canvas.Invalidate();
                return;
            }

            if (IsMenuVisible)
            {
                menuFullVisionHovered = menuFullVisionRect.Contains(e.Location);
                menuRestartHovered = menuRestartRect.Contains(e.Location);
                menuCloseHovered = menuCloseRect.Contains(e.Location);
                menuMainMenuHovered = menuMainMenuRect.Contains(e.Location);
                canvas.Invalidate();
                return;
            }

            if (ShowVictory || ShowDefeat)
            {
                btnRestartHovered = btnRestartRect.Contains(e.Location);
                btnExitHovered = btnExitRect.Contains(e.Location);
                canvas.Invalidate();
                return;
            }

            if (btnRestartHovered || btnExitHovered)
            {
                btnRestartHovered = false;
                btnExitHovered = false;
                canvas.Invalidate();
            }
        }

        public void HandleMouseUp(MouseEventArgs e)
        {
            
        }

        private void UpdateCellSize()
        {
            if (canvas.Width == 0 || canvas.Height == 0 || model == null) return;
            int maxCellW = canvas.Width / model.Maze.Width;
            int maxCellH = canvas.Height / model.Maze.Height;
            cellSize = Math.Min(maxCellW, maxCellH);
            if (cellSize < 5) cellSize = 5;

            int mazePixelW = cellSize * model.Maze.Width;
            int mazePixelH = cellSize * model.Maze.Height;
            offsetX = (canvas.Width - mazePixelW) / 2;
            offsetY = (canvas.Height - mazePixelH) / 2;
        }

        private void ScaleImages()
        {
            wallImageScaled?.Dispose();
            floorImageScaled?.Dispose();
            floorMemoryScaled?.Dispose();
            if (floorMemoryImage != null)
                floorMemoryScaled = new Bitmap(floorMemoryImage, cellSize, cellSize);
            else
                floorMemoryScaled = null;   
            exitImageScaled?.Dispose();
            playerImageScaled?.Dispose();
            patrolEnemyImageScaled?.Dispose();
            chaserEnemyImageScaled?.Dispose();
            itemMemoryImageScaled?.Dispose();
            itemVisionImageScaled?.Dispose();
            itemSlowImageScaled?.Dispose();

            if (wallImage != null) wallImageScaled = new Bitmap(wallImage, cellSize, cellSize);
            if (floorImage != null) floorImageScaled = new Bitmap(floorImage, cellSize, cellSize);
            if (exitImage != null) exitImageScaled = new Bitmap(exitImage, cellSize, cellSize);
            if (playerImage != null) playerImageScaled = new Bitmap(playerImage, cellSize, cellSize);
            if (patrolEnemyImage != null) patrolEnemyImageScaled = new Bitmap(patrolEnemyImage, cellSize, cellSize);
            if (chaserEnemyImage != null) chaserEnemyImageScaled = new Bitmap(chaserEnemyImage, cellSize, cellSize);
            if (itemMemoryImage != null) itemMemoryImageScaled = new Bitmap(itemMemoryImage, cellSize, cellSize);
            if (itemVisionImage != null) itemVisionImageScaled = new Bitmap(itemVisionImage, cellSize, cellSize);
            if (itemSlowImage != null) itemSlowImageScaled = new Bitmap(itemSlowImage, cellSize, cellSize);
        }

        private void UpdateButtonRects()
        {
            int w = 120, h = 35;
            int centerX = canvas.Width / 2;
            int centerY = canvas.Height / 2 + 20;
            btnRestartRect = new Rectangle(centerX - w - 10, centerY, w, h);
            btnExitRect = new Rectangle(centerX + 10, centerY, w, h);
        }
    }
}

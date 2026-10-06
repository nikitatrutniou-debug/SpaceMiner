using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SpaceMiner
{
    public partial class Form1 : Form
    {
        private readonly System.Windows.Forms.Timer gameTimer;
        private readonly Random random = new Random();
        private const int BlockSize = 32;
        private const int WorldWidth = 260;
        private const int WorldHeight = 150;
        private readonly int[,] world = new int[WorldWidth, WorldHeight];
        private readonly float[,] blockDamage = new float[WorldWidth, WorldHeight];
        private readonly List<Particle> particles = new List<Particle>();
        private double playerX = 130;
        private double playerY = 12;
        private double velocityX;
        private double velocityY;
        private double playerAngle;
        private double cameraX;
        private double cameraY;
        private bool thrust;
        private bool reverse;
        private bool rotateLeft;
        private bool rotateRight;
        private bool drilling;
        private int drillX = -1;
        private int drillY = -1;
        private float drillProgress;
        private int energy = 100;
        private int weight;
        private float engineTime;
        private float drillRotation;
        private float shake;
        private int selectedSlot;
        private readonly int[] inventory = { 0, 0, 0, 0, 0, 0 };
        private const double RotationSpeed = 0.055;
        private const double Acceleration = 0.014;
        private const double ReverseAcceleration = 0.006;
        private const double MaxSpeed = 0.34;
        private const double Friction = 0.985;

        public Form1()
        {
            InitializeComponent();
            Text = "Космический рудокоп";
            ClientSize = new Size(1280, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(7, 9, 16);
            KeyPreview = true;
            DoubleBuffered = true;
            GenerateWorld();
            gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
            MouseDown += Form1_MouseDown;
            MouseUp += Form1_MouseUp;
            MouseMove += Form1_MouseMove;
        }

        private void GenerateWorld()
        {
            for (int x = 0; x < WorldWidth; x++)
            {
                int surface = 15 + (int)(Math.Sin(x * 0.055) * 3) + random.Next(-2, 3);
                for (int y = 0; y < WorldHeight; y++)
                    world[x, y] = y < surface ? 0 : y < surface + 5 ? 1 : 2;
            }
            GenerateCaves();
            GenerateOreVeins(3, 650, 24, 85, 5);
            GenerateOreVeins(4, 280, 40, 115, 4);
            GenerateOreVeins(5, 100, 65, 140, 3);
            CreateStartingArea();
        }

        private void GenerateCaves()
        {
            bool[,] cave = new bool[WorldWidth, WorldHeight];
            for (int x = 1; x < WorldWidth - 1; x++)
                for (int y = 18; y < WorldHeight - 1; y++)
                    cave[x, y] = random.NextDouble() < 0.47;

            for (int pass = 0; pass < 5; pass++)
            {
                bool[,] next = new bool[WorldWidth, WorldHeight];
                for (int x = 1; x < WorldWidth - 1; x++)
                    for (int y = 18; y < WorldHeight - 1; y++)
                    {
                        int neighbors = CountCaveNeighbors(cave, x, y);
                        next[x, y] = neighbors >= 5 || neighbors > 3 && cave[x, y];
                    }
                cave = next;
            }

            for (int x = 1; x < WorldWidth - 1; x++)
                for (int y = 18; y < WorldHeight - 1; y++)
                    if (cave[x, y]) world[x, y] = 0;

            for (int i = 0; i < 180; i++)
            {
                int x = random.Next(5, WorldWidth - 5);
                int y = random.Next(25, WorldHeight - 5);
                int length = random.Next(10, 35);
                double angle = random.NextDouble() * Math.PI * 2;
                for (int j = 0; j < length; j++)
                {
                    int px = x + (int)(Math.Cos(angle) * j);
                    int py = y + (int)(Math.Sin(angle) * j);
                    if (px < 2 || px >= WorldWidth - 2 || py < 20 || py >= WorldHeight - 2) continue;
                    int radius = random.Next(1, 3);
                    for (int dx = -radius; dx <= radius; dx++)
                        for (int dy = -radius; dy <= radius; dy++)
                            if (dx * dx + dy * dy <= radius * radius) world[px + dx, py + dy] = 0;
                    angle += (random.NextDouble() - 0.5) * 0.45;
                }
            }
        }

        private int CountCaveNeighbors(bool[,] cave, int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int px = x + dx, py = y + dy;
                    if (px < 0 || px >= WorldWidth || py < 0 || py >= WorldHeight || cave[px, py]) count++;
                }
            return count;
        }

        private void GenerateOreVeins(int type, int amount, int minDepth, int maxDepth, int maxSize)
        {
            for (int i = 0; i < amount; i++)
            {
                int x = random.Next(3, WorldWidth - 3);
                int y = random.Next(minDepth, Math.Min(maxDepth, WorldHeight - 3));
                if (world[x, y] != 2) continue;
                GrowOreVein(type, x, y, random.Next(2, maxSize + 1));
            }
        }

        private void GrowOreVein(int type, int startX, int startY, int size)
        {
            Queue<Point> queue = new Queue<Point>();
            HashSet<Point> visited = new HashSet<Point>();
            queue.Enqueue(new Point(startX, startY));
            while (queue.Count > 0 && visited.Count < size)
            {
                Point point = queue.Dequeue();
                if (visited.Contains(point)) continue;
                if (point.X < 1 || point.X >= WorldWidth - 1 || point.Y < 20 || point.Y >= WorldHeight - 1) continue;
                visited.Add(point);
                if (world[point.X, point.Y] != 2) continue;
                world[point.X, point.Y] = type;
                if (random.NextDouble() < 0.85) queue.Enqueue(new Point(point.X + 1, point.Y));
                if (random.NextDouble() < 0.85) queue.Enqueue(new Point(point.X - 1, point.Y));
                if (random.NextDouble() < 0.85) queue.Enqueue(new Point(point.X, point.Y + 1));
                if (random.NextDouble() < 0.85) queue.Enqueue(new Point(point.X, point.Y - 1));
            }
        }

        private void CreateStartingArea()
        {
            for (int x = 120; x <= 140; x++)
                for (int y = 7; y <= 24; y++) world[x, y] = 0;
            for (int x = 115; x <= 145; x++)
                for (int y = 22; y <= 26; y++) if (random.NextDouble() < 0.75) world[x, y] = 1;
        }

        private void GameLoop(object sender, EventArgs e)
        {
            UpdatePhysics();
            UpdateDrilling();
            UpdateParticles();
            UpdateCamera();
            engineTime += 0.25f;
            drillRotation += 18f;
            shake = drilling ? Math.Min(2.5f, shake + 0.25f) : shake * 0.88f;
            Invalidate();
        }

        private void UpdatePhysics()
        {
            if (rotateLeft) playerAngle -= RotationSpeed;
            if (rotateRight) playerAngle += RotationSpeed;
            double weightModifier = Math.Max(0.45, 1.0 - Math.Min(weight, 100) * 0.004);
            double energyModifier = energy <= 10 ? 0.55 : 1.0;
            double acceleration = Acceleration * weightModifier * energyModifier;
            if (thrust && energy > 0)
            {
                velocityX += Math.Cos(playerAngle) * acceleration;
                velocityY += Math.Sin(playerAngle) * acceleration;
                energy = Math.Max(0, energy - 1);
            }
            if (reverse && energy > 0)
            {
                velocityX -= Math.Cos(playerAngle) * ReverseAcceleration;
                velocityY -= Math.Sin(playerAngle) * ReverseAcceleration;
                energy = Math.Max(0, energy - 1);
            }
            if (!thrust && !reverse) { velocityX *= Friction; velocityY *= Friction; }
            double maxSpeed = MaxSpeed * weightModifier * energyModifier;
            double speed = Math.Sqrt(velocityX * velocityX + velocityY * velocityY);
            if (speed > maxSpeed) { velocityX = velocityX / speed * maxSpeed; velocityY = velocityY / speed * maxSpeed; }
            double newX = playerX + velocityX, newY = playerY + velocityY;
            if (CanMoveTo(newX, playerY)) playerX = newX; else velocityX *= -0.25;
            if (CanMoveTo(playerX, newY)) playerY = newY; else velocityY *= -0.25;
            playerX = Math.Max(1, Math.Min(WorldWidth - 2, playerX));
            playerY = Math.Max(1, Math.Min(WorldHeight - 2, playerY));
        }

        private bool CanMoveTo(double x, double y)
        {
            double radius = 0.32;
            int left = (int)Math.Floor(x - radius), right = (int)Math.Floor(x + radius);
            int top = (int)Math.Floor(y - radius), bottom = (int)Math.Floor(y + radius);
            for (int bx = left; bx <= right; bx++)
                for (int by = top; by <= bottom; by++)
                {
                    if (bx < 0 || bx >= WorldWidth || by < 0 || by >= WorldHeight) return false;
                    if (world[bx, by] != 0) return false;
                }
            return true;
        }

        private void UpdateDrilling()
        {
            if (!drilling || drillX < 0 || drillY < 0) return;
            if (world[drillX, drillY] == 0) { ResetDrill(); return; }
            double dx = drillX + 0.5 - playerX, dy = drillY + 0.5 - playerY;
            if (Math.Sqrt(dx * dx + dy * dy) > 4.5) { ResetDrill(); return; }
            if (energy <= 0) return;
            drillProgress += GetDrillSpeed(world[drillX, drillY]);
            blockDamage[drillX, drillY] = drillProgress;
            SpawnDrillParticles();
            if (drillProgress >= 100)
            {
                int type = world[drillX, drillY];
                AddResource(type);
                SpawnBlockParticles(drillX, drillY, type);
                world[drillX, drillY] = 0;
                blockDamage[drillX, drillY] = 0;
                energy = Math.Max(0, energy - 3);
                ResetDrill();
            }
        }

        private float GetDrillSpeed(int type)
        {
            switch (type)
            {
                case 1: return 4.5f;
                case 2: return 2.4f;
                case 3: return 2.8f;
                case 4: return 1.9f;
                case 5: return 1.1f;
                default: return 1f;
            }
        }

        private void AddResource(int type)
        {
            int slot;
            switch (type)
            {
                case 1: slot = 0; break;
                case 2: slot = 1; break;
                case 3: slot = 2; break;
                case 4: slot = 3; break;
                case 5: slot = 4; break;
                default: return;
            }
            inventory[slot]++;
            weight += type == 3 ? 2 : type == 4 ? 3 : type == 5 ? 5 : 1;
            weight = Math.Min(100, weight);
        }

        private void ResetDrill() { drillX = -1; drillY = -1; drillProgress = 0; }

        private void SpawnDrillParticles()
        {
            if (random.NextDouble() > 0.45) return;
            double angle = playerAngle + Math.PI + (random.NextDouble() - 0.5) * 0.9;
            particles.Add(new Particle { X = playerX + Math.Cos(playerAngle) * 1.7, Y = playerY + Math.Sin(playerAngle) * 1.7, VX = Math.Cos(angle) * (0.02 + random.NextDouble() * 0.04), VY = Math.Sin(angle) * (0.02 + random.NextDouble() * 0.04), Life = 20 + random.Next(20), Size = random.Next(2, 5), Type = 0 });
        }

        private void SpawnBlockParticles(int blockX, int blockY, int type)
        {
            Color particleColor = type switch
            {
                1 => Color.FromArgb(170, 110, 70),
                2 => Color.FromArgb(135, 140, 150),
                3 => Color.FromArgb(60, 60, 65),
                4 => Color.FromArgb(230, 160, 75),
                5 => Color.FromArgb(90, 230, 245),
                _ => Color.White
            };
            for (int i = 0; i < 15; i++)
            {
                double angle = random.NextDouble() * Math.PI * 2, speed = 0.03 + random.NextDouble() * 0.09;
                particles.Add(new Particle { X = blockX + 0.5, Y = blockY + 0.5, VX = Math.Cos(angle) * speed, VY = Math.Sin(angle) * speed, Life = random.Next(25, 55), Size = random.Next(2, 5), Type = 1, Color = particleColor });
            }
        }

        private void UpdateParticles()
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                particle.X += particle.VX;
                particle.Y += particle.VY;
                particle.VY += 0.0015;
                particle.Life--;
                if (particle.Life <= 0) particles.RemoveAt(i);
            }
        }

        private void UpdateCamera()
        {
            cameraX = playerX * BlockSize - ClientSize.Width / 2;
            cameraY = playerY * BlockSize - ClientSize.Height / 2;
            cameraX = Math.Max(0, Math.Min(WorldWidth * BlockSize - ClientSize.Width, cameraX));
            cameraY = Math.Max(0, Math.Min(WorldHeight * BlockSize - ClientSize.Height, cameraY));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(7, 9, 16));
            DrawBackground(g);
            DrawWorld(g);
            DrawParticles(g);
            DrawDrillTarget(g);
            DrawPlayer(g);
            DrawHUD(g);
            DrawMinimap(g);
            DrawHotbar(g);
        }

        private void DrawBackground(Graphics g)
        {
            using Brush background = new SolidBrush(Color.FromArgb(7, 9, 16));
            g.FillRectangle(background, ClientRectangle);
            using Brush glow = new SolidBrush(Color.FromArgb(15, 22, 40));
            g.FillEllipse(glow, ClientSize.Width / 2 - 500, ClientSize.Height / 2 - 500, 1000, 1000);
        }

        private void DrawWorld(Graphics g)
        {
            int startX = Math.Max(0, (int)(cameraX / BlockSize) - 1);
            int startY = Math.Max(0, (int)(cameraY / BlockSize) - 1);
            int endX = Math.Min(WorldWidth, startX + ClientSize.Width / BlockSize + 3);
            int endY = Math.Min(WorldHeight, startY + ClientSize.Height / BlockSize + 3);
            for (int x = startX; x < endX; x++)
                for (int y = startY; y < endY; y++)
                {
                    int type = world[x, y];
                    if (type == 0) continue;
                    Rectangle rect = new Rectangle((int)(x * BlockSize - cameraX), (int)(y * BlockSize - cameraY), BlockSize + 1, BlockSize + 1);
                    Color c = type switch { 1 => Color.FromArgb(105, 65, 45), 2 => Color.FromArgb(70, 75, 85), 3 => Color.FromArgb(35, 38, 42), 4 => Color.FromArgb(150, 100, 40), 5 => Color.FromArgb(45, 150, 165), _ => Color.Gray };
                    using Brush b = new SolidBrush(c);
                    g.FillRectangle(b, rect);
                    using Pen p = new Pen(Color.FromArgb(35, 35, 45));
                    g.DrawRectangle(p, rect);
                    float damage = blockDamage[x, y];
                    if (damage > 0) { using Pen dp = new Pen(Color.FromArgb(220, 230, 240), 2); g.DrawRectangle(dp, rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6); }
                }
        }

        private void DrawParticles(Graphics g)
        {
            foreach (Particle particle in particles)
            {
                float alpha = Math.Max(0, Math.Min(255, particle.Life * 8));
                Color c = particle.Type == 1 ? Color.FromArgb((int)alpha, particle.Color) : Color.FromArgb((int)alpha, 220, 220, 220);
                using Brush b = new SolidBrush(c);
                float sx = (float)(particle.X * BlockSize - cameraX);
                float sy = (float)(particle.Y * BlockSize - cameraY);
                g.FillEllipse(b, sx, sy, particle.Size, particle.Size);
            }
        }

        private void DrawDrillTarget(Graphics g)
        {
            if (!drilling || drillX < 0 || drillY < 0) return;
            int sx = (int)(drillX * BlockSize - cameraX), sy = (int)(drillY * BlockSize - cameraY);
            using Pen p = new Pen(Color.FromArgb(220, 245, 255), 2);
            g.DrawRectangle(p, sx + 2, sy + 2, BlockSize - 4, BlockSize - 4);
        }

        private void DrawPlayer(Graphics g)
        {
            float cx = (float)(playerX * BlockSize - cameraX), cy = (float)(playerY * BlockSize - cameraY);
            GraphicsState state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform((float)(playerAngle * 180 / Math.PI));
            if (thrust) { using Brush flame = new SolidBrush(Color.FromArgb(240, 255, 130)); PointF[] pts = { new PointF(-28, -8), new PointF(-55 - (float)(Math.Sin(engineTime) * 8), 0), new PointF(-28, 8) }; g.FillPolygon(flame, pts); }
            using Brush body = new SolidBrush(Color.FromArgb(215, 225, 235));
            PointF[] ship = { new PointF(28, 0), new PointF(-18, -18), new PointF(-12, 0), new PointF(-18, 18) };
            g.FillPolygon(body, ship);
            using Brush cockpit = new SolidBrush(Color.FromArgb(40, 110, 145));
            g.FillEllipse(cockpit, -5, -9, 16, 18);
            using Pen outline = new Pen(Color.FromArgb(20, 25, 35), 2);
            g.DrawPolygon(outline, ship);
            g.Restore(state);
        }

        private void DrawHUD(Graphics g)
        {
            using Font font = new Font("Segoe UI", 12, FontStyle.Bold);
            using Brush text = new SolidBrush(Color.White);
            g.DrawString("ЭНЕРГИЯ", font, text, 18, 16);
            using Brush energyBg = new SolidBrush(Color.FromArgb(45, 45, 55));
            g.FillRectangle(energyBg, 18, 43, 230, 18);
            using Brush energyFill = new SolidBrush(Color.FromArgb(75, 205, 125));
            g.FillRectangle(energyFill, 18, 43, 230 * energy / 100, 18);
            g.DrawString($"{energy}%", font, text, 255, 40);
            g.DrawString($"Вес: {weight}/100", font, text, 18, 75);
            g.DrawString("W/S — тяга  A/D — поворот  ЛКМ — бур", new Font("Segoe UI", 10), text, 18, 105);
        }

        private void DrawMinimap(Graphics g)
        {
            int size = 150, ox = ClientSize.Width - size - 20, oy = 20;
            using Brush bg = new SolidBrush(Color.FromArgb(170, 10, 12, 20));
            g.FillRectangle(bg, ox, oy, size, size);
            for (int x = 0; x < WorldWidth; x += 3)
                for (int y = 0; y < WorldHeight; y += 3)
                    if (world[x, y] != 0)
                    {
                        using Brush b = new SolidBrush(Color.FromArgb(90, 120, 130, 145));
                        g.FillRectangle(b, ox + x * size / WorldWidth, oy + y * size / WorldHeight, 2, 2);
                    }
            using Brush player = new SolidBrush(Color.White);
            g.FillEllipse(player, ox + (float)playerX * size / WorldWidth - 2, oy + (float)playerY * size / WorldHeight - 2, 5, 5);
            using Pen p = new Pen(Color.FromArgb(100, 180, 200), 1);
            g.DrawRectangle(p, ox, oy, size, size);
        }

        private void DrawHotbar(Graphics g)
        {
            int slotSize = 58, total = slotSize * 6, x0 = (ClientSize.Width - total) / 2, y0 = ClientSize.Height - 78;
            string[] names = { "З", "К", "У", "Ж", "А", "" };
            for (int i = 0; i < 6; i++)
            {
                using Brush bg = new SolidBrush(i == selectedSlot ? Color.FromArgb(80, 130, 170) : Color.FromArgb(45, 50, 60));
                g.FillRectangle(bg, x0 + i * slotSize, y0, slotSize - 4, slotSize - 4);
                using Pen p = new Pen(Color.FromArgb(120, 130, 145));
                g.DrawRectangle(p, x0 + i * slotSize, y0, slotSize - 4, slotSize - 4);
                using Font f = new Font("Segoe UI", 14, FontStyle.Bold);
                using Brush t = new SolidBrush(Color.White);
                g.DrawString(names[i], f, t, x0 + i * slotSize + 21, y0 + 7);
                g.DrawString(inventory[i].ToString(), new Font("Segoe UI", 9), t, x0 + i * slotSize + 8, y0 + 35);
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) thrust = true;
            if (e.KeyCode == Keys.S) reverse = true;
            if (e.KeyCode == Keys.A) rotateLeft = true;
            if (e.KeyCode == Keys.D) rotateRight = true;
            if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D6) selectedSlot = e.KeyCode - Keys.D1;
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W) thrust = false;
            if (e.KeyCode == Keys.S) reverse = false;
            if (e.KeyCode == Keys.A) rotateLeft = false;
            if (e.KeyCode == Keys.D) rotateRight = false;
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            double worldX = (e.X + cameraX) / BlockSize;
            double worldY = (e.Y + cameraY) / BlockSize;
            drillX = (int)Math.Floor(worldX);
            drillY = (int)Math.Floor(worldY);
            drilling = true;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { drilling = false; ResetDrill(); }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (!drilling) return;
            drillX = (int)Math.Floor((e.X + cameraX) / BlockSize);
            drillY = (int)Math.Floor((e.Y + cameraY) / BlockSize);
        }

        private class Particle
        {
            public double X;
            public double Y;
            public double VX;
            public double VY;
            public int Life;
            public int Size;
            public int Type;
            public Color Color;
        }
    }
}
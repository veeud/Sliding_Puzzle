using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace SlidingPuzzle
{
    public partial class MainWindow : Window
    {
        private bool firstClick = false;
        private bool sliderThumbHeld = false;
        private List<ConfettiItem> confettiList = new List<ConfettiItem>();
        private int dimension = 3;
        private Dictionary<int, Point> puzzle = new Dictionary<int, Point>();
        private List<Button> squares = new List<Button>();
        private DateTime startTime;
        private Random rng = new Random();
        readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        readonly TimerModel timerModel = new TimerModel();

        public MainWindow()
        {
            InitializeComponent();
            TimeBlock.DataContext = timerModel;
            timer.Interval = 30;
            timer.Tick += Timer_Tick;
            ConfettiCanvas.SizeChanged += ConfettiCanvas_SizeChanged;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var timeSpan = DateTime.Now.Subtract(startTime);
            if (timeSpan < TimeSpan.FromHours(1))
                timerModel.Timer = string.Format("{0:mm\\:ss\\:ff}", timeSpan);
            else if (timeSpan < TimeSpan.FromHours(24))
                timerModel.Timer = string.Format("{0:hh\\:mm\\:ss}", timeSpan);
            else
                timerModel.Timer = ((int)timeSpan.TotalHours).ToString() + ":" + string.Format("{0:mm\\:ss}", timeSpan);
        }


        private void Start()
        {
            timer.Stop();
            timerModel.Timer = "00:00:00";
            DefaultArrangement();
            Shuffle();
            Visualize();
        }

        private void Visualize()
        {
            for (int i = 1; i < dimension * dimension; i++)
            {
                Button btn = squares.FirstOrDefault(n => (int)n.Tag == i);
                MoveSquare(btn, (int)puzzle[i].X, (int)puzzle[i].Y);
            }
        }


        private void Shuffle()
        {
            for (int i = 0; i < dimension * 100; i++)
            {
                var curr = puzzle.FirstOrDefault(pair => pair.Value == puzzle[0]);
                List<Direction> possibleDir = new List<Direction>();

                if (curr.Value.Y > 0)
                    possibleDir.Add(Direction.North);
                if (curr.Value.Y < dimension - 1)
                    possibleDir.Add(Direction.South);
                if (curr.Value.X > 0)
                    possibleDir.Add(Direction.West);
                if (curr.Value.X < dimension - 1)
                    possibleDir.Add(Direction.East);

                int rand = rng.Next(0, possibleDir.Count);
                Direction dir = possibleDir[rand];

                Point p = new Point();

                if (dir == Direction.North)
                    p = new Point(curr.Value.X, curr.Value.Y - 1);
                else if (dir == Direction.South)
                    p = new Point(curr.Value.X, curr.Value.Y + 1);
                else if (dir == Direction.West)
                    p = new Point(curr.Value.X - 1, curr.Value.Y);
                else if (dir == Direction.East)
                    p = new Point(curr.Value.X + 1, curr.Value.Y);

                var dest = puzzle.First(square => square.Value == p);
                puzzle[dest.Key] = curr.Value;
                puzzle[curr.Key] = dest.Value;
            }
        }

        private void DefaultArrangement()
        {
            for (int i = 0; i < dimension; i++)
            {
                ColumnDefinition colDef = new ColumnDefinition();
                RowDefinition rowDef = new RowDefinition();
                Grid.ColumnDefinitions.Add(colDef);
                Grid.RowDefinitions.Add(rowDef);

            }
            for (int i = 0; i < dimension * dimension - 1; i++)
            {
                int num = i + 1;
                puzzle.Add(num, new Point(i % dimension, i / dimension));
                Button btn = new Button();
                btn.HorizontalAlignment = HorizontalAlignment.Stretch;
                btn.VerticalAlignment = VerticalAlignment.Stretch;
                btn.Tag = num;
                btn.Template = (ControlTemplate)Resources["ButtonTemplate"];
                Grid.Children.Add(btn);
                squares.Add(btn);
            }
            puzzle.Add(0, new Point(dimension - 1, dimension - 1));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int num = (int)((Button)sender).Tag;
            Point curr = puzzle[num];
            Point dest = puzzle[0];

            if ((curr.X - 1 == dest.X && curr.Y == dest.Y) || (curr.X + 1 == dest.X && curr.Y == dest.Y) || (curr.X == dest.X && curr.Y - 1 == dest.Y) || (curr.X == dest.X && curr.Y + 1 == dest.Y))
            {
                if (!firstClick)
                {
                    startTime = DateTime.Now;
                    timer.Start();
                    firstClick = true;
                }
                Task.Run(() => PlaySound(Properties.Resources.Move));
                int count = Convert.ToInt32(MoveCounter.Text) + 1;
                MoveCounter.Text = count.ToString();
                Button btn = squares.FirstOrDefault(n => n.Tag == ((Button)sender).Tag);
                MoveSquare(btn, (int)dest.X, (int)dest.Y);
                puzzle[num] = dest;
                puzzle[0] = curr;
                CheckArrangement();
            }
            else
            {
                Task.Run(() => PlaySound(Properties.Resources.Wrong));
            }
        }

        private void CheckArrangement()
        {
            bool correct = true;
            for (int i = 0; i < puzzle.Count - 1; i++)
            {
                if (puzzle.ElementAt(i).Value != new Point(i % dimension, i / dimension))
                    correct = false;
            }
            if (correct)
            {
                timer.Stop();
                Task.Run(() => PlaySound(Properties.Resources.Win));
                GenerateConfetti();
                ModernWpf.MessageBox.Show("Congratulations on your incredible victory,\nachieving a time of: " + timerModel.Timer + " and " + MoveCounter.Text + " moves!");
                Restart();
            }
        }

        private void Restart()
        {
            confettiList.Clear();
            ConfettiCanvas.Children.Clear();
            Grid.ColumnDefinitions.Clear();
            Grid.RowDefinitions.Clear();
            puzzle.Clear();
            squares.Clear();
            Grid.Children.Clear();
            MoveCounter.Text = "0";
            firstClick = false;
            Start();
        }

        private void PlaySound(UnmanagedMemoryStream sound)
        {
            SoundPlayer player = new SoundPlayer(sound);
            player.Play();
        }



        private void MoveSquare(Button button, int col, int row)
        {
            Grid.SetColumn(button, col);
            Grid.SetRow(button, row);
        }

        enum Direction
        {
            North,
            South,
            West,
            East
        }

        private void ConfettiCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            confettiList.Clear();
            ConfettiCanvas.Children.Clear();
        }

        private void GenerateConfetti()
        {
            double canvasHeight = ConfettiCanvas.ActualHeight;
            double canvasWidth = ConfettiCanvas.ActualWidth;
            int amount = (int)Math.Pow(this.Width / 100, 2);
            for (int i = 0; i < amount; i++)
            {
                Shape confetti = CreateConfetti();
                ConfettiItem confettiItem = new ConfettiItem { Shape = confetti, Counter = 0 };
                confettiList.Add(confettiItem);
                ConfettiCanvas.Children.Add(confetti);

                double left = rng.NextDouble() * (canvasWidth - confetti.Width);
                double top = -confetti.Height;

                Canvas.SetLeft(confetti, left);
                Canvas.SetTop(confetti, top);
            }

            AnimateConfetti(canvasHeight);
        }

        private void AnimateConfetti(double canvasHeight)
        {
            foreach (ConfettiItem confettiItem in confettiList)
            {
                Shape confetti = confettiItem.Shape;

                if (confettiItem.Counter < 3)
                {
                    double duration = 6;

                    DoubleAnimation animation = new DoubleAnimation
                    {
                        From = -confetti.ActualHeight,
                        To = canvasHeight,
                        Duration = TimeSpan.FromSeconds(duration),
                        AutoReverse = false,
                        RepeatBehavior = new RepeatBehavior(3),
                        BeginTime = TimeSpan.FromSeconds(rng.NextDouble() * duration)
                    };

                    confetti.BeginAnimation(Canvas.TopProperty, animation);
                    confettiItem.Counter++;
                }
            }
        }


        private Shape CreateConfetti()
        {
            Shape confetti = new Ellipse
            {
                Width = 15,
                Height = 15,
                Fill = new SolidColorBrush(Color.FromRgb((byte)rng.Next(256), (byte)rng.Next(256), (byte)rng.Next(256)))
            };

            return confetti;
        }

        private class ConfettiItem
        {
            public Shape Shape { get; set; }
            public int Counter { get; set; }
        }

        private void SizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!sliderThumbHeld)
                NewPuzzle();
        }

        private void NewPuzzle()
        {
            dimension = (int)SizeSlider.Value;
            if (dimension > 9)
                WindowState = WindowState.Maximized;

            Restart();
        }

        private void SizeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            sliderThumbHeld = true;
        }

        private void SizeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            sliderThumbHeld = false;
            if(SizeSlider.Value !=  dimension)
                NewPuzzle();
        }
    }
}

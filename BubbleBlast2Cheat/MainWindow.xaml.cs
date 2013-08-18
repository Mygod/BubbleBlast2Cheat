using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Mygod.BubbleBlast2.Cheat
{
    public sealed partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            for (var i = 0; i < 30; i++) 
            {
                var rct = new Rectangle { Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)), Tag = i };
                rct.MouseUp += SetColor;
                Shower.Children.Add(rct);
            }
            Redraw(null, null);
        }

        private void NumbersOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
            var target = MostStepsBox.Text.Remove(MostStepsBox.SelectionStart, MostStepsBox.SelectionLength)
                                          .Insert(MostStepsBox.SelectionStart, e.Text);
            uint i;
            e.Handled = !uint.TryParse(target, out i);
        }

        private void NegativableNumbersOnly(object sender, TextCompositionEventArgs e)
        {
            var target = SolutionNumberBox.Text.Remove(SolutionNumberBox.SelectionStart, SolutionNumberBox.SelectionLength)
                                               .Insert(SolutionNumberBox.SelectionStart, e.Text);
            int i;
            e.Handled = !int.TryParse(target, out i);
        }

        private byte[,] startStatus = new byte[6,5];
        private static readonly SolidColorBrush[] Colors = new[] 
            { new SolidColorBrush(Color.FromRgb(255, 255, 255)), new SolidColorBrush(Color.FromRgb(255, 0, 0)), 
              new SolidColorBrush(Color.FromRgb(0, 255, 0)),     new SolidColorBrush(Color.FromRgb(255, 255, 0)), 
              new SolidColorBrush(Color.FromRgb(0, 0, 255)) };

        public class StepCompressed
        {
            private StepCompressed(StepCompressed step) : this(step.clickX, step.clickY, step.lastStep)
            {
            }
            protected StepCompressed(int clickX = -1, int clickY = -1, StepCompressed lastStep = null)
            {
                this.clickX = (sbyte)clickX;
                this.clickY = (sbyte)clickY;
                this.lastStep = lastStep == null ? null : new StepCompressed(lastStep);
            }

            private readonly sbyte clickX, clickY;
            private readonly StepCompressed lastStep;

            public string GetResult()
            {
                var result = string.Empty;
                var step = this;
                while (step.lastStep != null)
                {
                    if (!string.IsNullOrEmpty(result)) result = " => " + result;
                    result = string.Format("({0},{1})", step.clickX + 1, step.clickY + 1) + result;
                    step = step.lastStep;
                }
                return result;
            }
        }
        private sealed class Step : StepCompressed
        {
            public Step(byte[,] status, int stepNumber, int score, int clickX = -1, int clickY = -1, StepCompressed lastStep = null)
                : base(clickX, clickY, lastStep)
            {
                Status = status;
                StepNumber = stepNumber;
                Score = score;
            }
            public readonly byte[,] Status;
            public readonly int StepNumber, Score;
        }

        private void Redraw(object sender, SizeChangedEventArgs e)
        {
            var i = 0;
            Shower.ItemWidth = Shower.ActualWidth/5;
            Shower.ItemHeight = Shower.ActualHeight/6;
            foreach (Rectangle r in Shower.Children)
            {
                r.Fill = Colors[startStatus[i / 5, i % 5]];
                i++;
            }
        }

        private void SetColor(object sender, MouseButtonEventArgs e)
        {
            var tag = (int)((Rectangle)sender).Tag;
            if (e.ChangedButton == MouseButton.Middle) Play(startStatus, tag / 5, tag % 5);
            else startStatus[tag / 5, tag % 5] = (byte)
                (e.ChangedButton == MouseButton.Right ? 0 : ((startStatus[tag / 5, tag % 5] + 1) % 5));
            Redraw(sender, null);
        }

        private void SetColor(object sender, MouseWheelEventArgs e)
        {
            var tag = (from IInputElement i in Shower.Children select Mouse.GetPosition(i))
                      .TakeWhile(p => p.X < 0 || p.Y < 0 || p.X > Shower.ItemWidth || p.Y > Shower.ItemHeight).Count();
            if (tag < 0 || tag >= 30) return;
            startStatus[tag / 5, tag % 5] = (byte) ((10 + startStatus[tag / 5, tag % 5] - e.Delta / Mouse.MouseWheelDeltaForOneLine) % 5);
            Redraw(sender, null);
        }

        private void GetSolution(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MostStepsBox.Text)) return;
            int mostSteps = int.Parse(MostStepsBox.Text), needSolutions = int.Parse(SolutionNumberBox.Text);
            if (needSolutions == 0) return;
            var queue = new LinkedList<Step>();
            var solutions = new List<Tuple<int, int, string>>();
            queue.AddFirst(new Step(startStatus, 0, 0));
            while (queue.Count > 0)
            {
                var lastStep = queue.First.Value;
                var nowStep = lastStep.StepNumber + 1;
                queue.RemoveFirst();
                for (var x = 0; x < 6; x++) for (var y = 0; y < 5; y++) if (lastStep.Status[x, y] > 0)
                {
                    var status = (byte[,])lastStep.Status.Clone();
                    var score = lastStep.Score + Play(status, x, y);
                    if (IsFinal(status))
                    {
                        solutions.Add(new Tuple<int, int, string>(nowStep, score, 
                                                                 new Step(status, nowStep, score, x, y, lastStep).GetResult()));
                        if (solutions.Count == needSolutions) goto end;
                        continue;
                    }
                    if (nowStep < mostSteps) queue.AddLast(new Step(status, nowStep, score, x, y, lastStep));
                }
            }
        end:
            new TextWindow("结束", string.Format("求解完毕，共找到 {0} 种解法。部分解以及估分可能不正确，仅供参考。{1}", solutions.Count, 
                Environment.NewLine) + string.Join(Environment.NewLine, from solution in solutions orderby solution.Item2 descending
                    select string.Format("{0} 步\t{1} 分\t{2}", solution.Item1, solution.Item2, solution.Item3))).ShowDialog();
        }

        private enum Direction : byte
        {
            Up, Right, Down, Left
        }
        private sealed class MovingBubble
        {
            public MovingBubble(int x, int y, Direction direction)
            {
                X = x;
                Y = y;
                this.direction = direction;
            }

            public int X, Y;
            private readonly Direction direction;

            public void Move()
            {
                switch (direction)
                {
                    case Direction.Up:
                        Y--;
                        break;
                    case Direction.Right:
                        X++;
                        break;
                    case Direction.Down:
                        Y++;
                        break;
                    case Direction.Left:
                        X--;
                        break;
                }
            }
        }

        private static int Play(byte[,] status, int x, int y)
        {
            if (status[x, y] == 0) return 0;
            status[x, y]--;
            if (status[x, y] > 0) return 0;
            int combo = 1, score = 10;
            var bubbles = new LinkedList<MovingBubble>();
            AddBubbles(bubbles, x, y);
            while (bubbles.Count > 0)
            {
                var ptr = bubbles.First;
                while (ptr != null)
                {
                    var next = ptr.Next;
                    ptr.Value.Move();
                    if (ptr.Value.X < 0 || ptr.Value.X >= 6 || ptr.Value.Y < 0 || ptr.Value.Y >= 5) bubbles.Remove(ptr);
                    else if (status[ptr.Value.X, ptr.Value.Y] > 0)
                    {
                        status[ptr.Value.X, ptr.Value.Y]--;
                        bubbles.Remove(ptr);
                        if (status[ptr.Value.X, ptr.Value.Y] == 0)
                        {
                            AddBubbles(bubbles, ptr.Value.X, ptr.Value.Y);
                            score += 10 + combo++;
                        }
                    }
                    ptr = next;
                }
            }
            return score;
        }
        private static void AddBubbles(LinkedList<MovingBubble> bubbles, int x, int y)
        {
            bubbles.AddFirst(new MovingBubble(x, y, Direction.Left));
            bubbles.AddFirst(new MovingBubble(x, y, Direction.Right));
            bubbles.AddFirst(new MovingBubble(x, y, Direction.Up));
            bubbles.AddFirst(new MovingBubble(x, y, Direction.Down));
        }

        private static bool IsFinal(byte[,] status)
        {
// ReSharper disable LoopCanBeConvertedToQuery
            foreach (var b in status) if (b != 0) return false;
// ReSharper restore LoopCanBeConvertedToQuery
            return true;
        }

        private void ClearAll(object sender, MouseButtonEventArgs e)
        {
            startStatus = new byte[6, 5];
            Redraw(sender, null);
        }
    }
}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IndustrialControlHMI.Models.Flowchart;

namespace IndustrialControlHMI.Views
{
    /// <summary>
    /// 流程图连接线控件（支持直角连接）
    /// </summary>
    public partial class FlowLineControl : UserControl
    {
        public static readonly DependencyProperty FlowLineModelProperty =
            DependencyProperty.Register("FlowLineModel", typeof(FlowLineModel), typeof(FlowLineControl),
                new PropertyMetadata(null, OnFlowLineModelChanged));

        /// <summary>
        /// 获取或设置连接线模型
        /// </summary>
        public FlowLineModel? FlowLineModel
        {
            get => (FlowLineModel?)GetValue(FlowLineModelProperty);
            set => SetValue(FlowLineModelProperty, value);
        }

        public FlowLineControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[FlowLineControl] Loaded, FlowLineModel={(FlowLineModel?.Id ?? "null")}");
            UpdateLine();
        }

        private static void OnFlowLineModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FlowLineControl control)
            {
                control.UpdateLine();
            }
        }

        private void UpdateLine()
        {
            if (FlowLineModel == null || LinePath == null) return;

            var m = FlowLineModel;
            
            // 验证起点和终点是否有效
            if (!IsValidPoint(m.Start) || !IsValidPoint(m.End))
            {
                System.Diagnostics.Debug.WriteLine($"[FlowLineControl] 跳过无效线条: {m.Id}");
                LinePath.Visibility = Visibility.Collapsed;
                if (ArrowPath != null) ArrowPath.Visibility = Visibility.Collapsed;
                return;
            }
            
            LinePath.Visibility = Visibility.Visible;
            LinePath.Stroke = new SolidColorBrush(m.LineColorWithStatus);
            LinePath.StrokeThickness = m.Thickness;
            LinePath.StrokeDashArray = m.DashStyle;
            LinePath.StrokeDashOffset = m.AnimationOffset;

            // 根据是否为直角线选择绘制方式
            System.Diagnostics.Debug.WriteLine($"[FlowLineControl] Line {m.Id}: IsOrthogonal={m.IsOrthogonal}, Start=({m.Start.X:F0},{m.Start.Y:F0}), End=({m.End.X:F0},{m.End.Y:F0}), IntermediatePoints={m.IntermediatePoints.Count}");
            
            if (m.IsOrthogonal)
            {
                System.Diagnostics.Debug.WriteLine($"[FlowLineControl] 绘制直角线: {m.Id}");
                DrawOrthogonalLine(m);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[FlowLineControl] 绘制斜线: {m.Id}");
                DrawDiagonalLine(m);
            }

            UpdateArrowPoints();
        }
        
        /// <summary>
        /// 绘制对角线（直接连接）
        /// </summary>
        private void DrawDiagonalLine(FlowLineModel m)
        {
            var length = Math.Sqrt(Math.Pow(m.End.X - m.Start.X, 2) + Math.Pow(m.End.Y - m.Start.Y, 2));
            if (length < 0.001)
            {
                LinePath.Visibility = Visibility.Collapsed;
                return;
            }
            LinePath.Data = new LineGeometry(m.Start, m.End);
        }
        
        /// <summary>
        /// 绘制直角线（横平竖直）
        /// </summary>
        private void DrawOrthogonalLine(FlowLineModel m)
        {
            var points = m.GetPathPoints();
            System.Diagnostics.Debug.WriteLine($"[DrawOrthogonalLine] 初始点数: {points.Count}");
            
            if (points.Count < 2)
            {
                LinePath.Visibility = Visibility.Collapsed;
                return;
            }
            
            // 如果只有两个点，自动计算中间转折点形成直角
            if (points.Count == 2)
            {
                var start = points[0];
                var end = points[1];
                
                // 计算中间点：先水平后垂直，或者先垂直后水平
                // 选择拐点：取X和Y的中点
                var midX = (start.X + end.X) / 2;
                var midY = (start.Y + end.Y) / 2;
                
                // 判断哪个方向距离更长，优先沿长方向走
                Point midPoint;
                if (Math.Abs(end.X - start.X) > Math.Abs(end.Y - start.Y))
                {
                    // 水平距离更长：先水平到中间X，再垂直到终点Y，再水平到终点X
                    midPoint = new Point(midX, start.Y);
                    points.Insert(1, midPoint);
                    points.Insert(2, new Point(midX, end.Y));
                    System.Diagnostics.Debug.WriteLine($"[DrawOrthogonalLine] 水平优先: 添加中间点 ({midX:F0},{start.Y:F0}) 和 ({midX:F0},{end.Y:F0})");
                }
                else
                {
                    // 垂直距离更长：先垂直到中间Y，再水平到终点X，再垂直到终点Y
                    midPoint = new Point(start.X, midY);
                    points.Insert(1, midPoint);
                    points.Insert(2, new Point(end.X, midY));
                    System.Diagnostics.Debug.WriteLine($"[DrawOrthogonalLine] 垂直优先: 添加中间点 ({start.X:F0},{midY:F0}) 和 ({end.X:F0},{midY:F0})");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[DrawOrthogonalLine] 最终点数: {points.Count}");
            
            // 创建路径几何
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure
            {
                StartPoint = points[0],
                IsClosed = false
            };
            
            // 添加线段
            for (int i = 1; i < points.Count; i++)
            {
                pathFigure.Segments.Add(new LineSegment(points[i], true));
            }
            
            pathGeometry.Figures.Add(pathFigure);
            LinePath.Data = pathGeometry;
        }
        
        /// <summary>
        /// 检查点坐标是否有效（非NaN、非Infinity）
        /// </summary>
        private bool IsValidPoint(Point p)
        {
            return !double.IsNaN(p.X) && !double.IsNaN(p.Y) 
                && !double.IsInfinity(p.X) && !double.IsInfinity(p.Y);
        }

        private void UpdateArrowPoints()
        {
            if (ArrowPath == null) return;
            if (FlowLineModel == null)
            {
                ArrowPath.Visibility = Visibility.Collapsed;
                return;
            }

            ArrowPath.Visibility = FlowLineModel.ShowArrow ? Visibility.Visible : Visibility.Collapsed;
            if (!FlowLineModel.ShowArrow) return;
            
            // 检查是否为水平线段（Y方向变化很小）
            // 对于直角线，可能有多个线段，需要检查最后一段
            if (FlowLineModel.IsOrthogonal && FlowLineModel.IntermediatePoints.Count > 0)
            {
                // 对于直角线，检查最后一段是否为水平
                var start = FlowLineModel.IntermediatePoints[FlowLineModel.IntermediatePoints.Count - 1];
                var end = FlowLineModel.End;
                var deltaX = Math.Abs(end.X - start.X);
                var deltaY = Math.Abs(end.Y - start.Y);
                
                // 如果最后一段是水平线（X变化远大于Y变化）
                if (deltaX > 5.0 && deltaY < 5.0)
                {
                    ArrowPath.Visibility = Visibility.Collapsed;
                    return;
                }
            }
            else
            {
                // 对于普通线，检查整体是否为水平
                var deltaX = Math.Abs(FlowLineModel.End.X - FlowLineModel.Start.X);
                var deltaY = Math.Abs(FlowLineModel.End.Y - FlowLineModel.Start.Y);
                
                // 如果是水平线（X变化远大于Y变化）
                if (deltaX > 5.0 && deltaY < 5.0)
                {
                    ArrowPath.Visibility = Visibility.Collapsed;
                    return;
                }
            }
            
            // 验证箭头参数
            var arrowSize = FlowLineModel.ArrowSize;
            if (arrowSize <= 0 || double.IsNaN(arrowSize) || double.IsInfinity(arrowSize))
            {
                ArrowPath.Visibility = Visibility.Collapsed;
                return;
            }
            
            var tip = FlowLineModel.End;
            if (!IsValidPoint(tip))
            {
                ArrowPath.Visibility = Visibility.Collapsed;
                return;
            }

            // 计算箭头方向（使用最后一段线的方向）
            double angle = CalculateLastSegmentAngle();
            if (double.IsNaN(angle) || double.IsInfinity(angle))
            {
                ArrowPath.Visibility = Visibility.Collapsed;
                return;
            }

            var leftAngle = angle + Math.PI * 0.75;
            var rightAngle = angle - Math.PI * 0.75;

            var leftPoint = new Point(
                tip.X + arrowSize * Math.Cos(leftAngle),
                tip.Y + arrowSize * Math.Sin(leftAngle));
            var rightPoint = new Point(
                tip.X + arrowSize * Math.Cos(rightAngle),
                tip.Y + arrowSize * Math.Sin(rightAngle));
            
            // 验证计算出的点
            if (!IsValidPoint(leftPoint) || !IsValidPoint(rightPoint))
            {
                ArrowPath.Visibility = Visibility.Collapsed;
                return;
            }

            SetValue(ArrowLeftPointProperty, leftPoint);
            SetValue(ArrowRightPointProperty, rightPoint);

            // 箭头几何在代码中设置，避免 XAML 绑定初始化异常
            var figure = new PathFigure
            {
                StartPoint = tip,
                IsClosed = true,
                IsFilled = true
            };
            figure.Segments.Add(new LineSegment(leftPoint, true));
            figure.Segments.Add(new LineSegment(rightPoint, true));
            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            ArrowPath.Data = geometry;
            ArrowPath.Fill = new SolidColorBrush(FlowLineModel.LineColorWithStatus);
        }
        
        /// <summary>
        /// 计算最后一段线的角度（用于箭头方向）
        /// </summary>
        private double CalculateLastSegmentAngle()
        {
            if (FlowLineModel == null) return 0;
            
            Point start, end;
            
            if (FlowLineModel.IsOrthogonal && FlowLineModel.IntermediatePoints.Count > 0)
            {
                // 直角线：使用最后一个中间点到终点的方向
                start = FlowLineModel.IntermediatePoints[FlowLineModel.IntermediatePoints.Count - 1];
                end = FlowLineModel.End;
            }
            else
            {
                // 普通线：使用起点到终点的方向
                start = FlowLineModel.Start;
                end = FlowLineModel.End;
            }
            
            var deltaX = end.X - start.X;
            var deltaY = end.Y - start.Y;
            return Math.Atan2(deltaY, deltaX);
        }

        #region 箭头点依赖属性（用于XAML绑定）

        public static readonly DependencyProperty ArrowLeftPointProperty =
            DependencyProperty.Register("ArrowLeftPoint", typeof(Point), typeof(FlowLineControl),
                new PropertyMetadata(new Point()));

        public static readonly DependencyProperty ArrowRightPointProperty =
            DependencyProperty.Register("ArrowRightPoint", typeof(Point), typeof(FlowLineControl),
                new PropertyMetadata(new Point()));

        /// <summary>
        /// 箭头左侧点
        /// </summary>
        public Point ArrowLeftPoint
        {
            get => (Point)GetValue(ArrowLeftPointProperty);
            set => SetValue(ArrowLeftPointProperty, value);
        }

        /// <summary>
        /// 箭头右侧点
        /// </summary>
        public Point ArrowRightPoint
        {
            get => (Point)GetValue(ArrowRightPointProperty);
            set => SetValue(ArrowRightPointProperty, value);
        }

        #endregion
    }
}

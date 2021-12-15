using DevExpress.XtraBars;
using DevExpress.XtraCharts;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;

namespace DXApplication5
{
    public partial class Form1 : DevExpress.XtraEditors.XtraForm
    {
        public Form1()
        {
            InitializeComponent();

            JObject o1 = JObject.Parse(File.ReadAllText(@"C:\Users\signes\Downloads\process-2AED09.2AED09-DRE-PC2.KPE5155000110506.L2E9B28A7AA.L2E9B28A713.201912090622.json"));

            //// read JSON directly from a file
            //using (StreamReader file = File.OpenText(@"c:\videogames.json"))
            //using (JsonTextReader reader = new JsonTextReader(file))
            //{
            //    JObject o2 = (JObject)JToken.ReadFrom(reader);
            //}

            DataTable table = _GenerateChart(o1);
            chartControl1.DataSource = table;

            _GenerateSeries();

            foreach(Series series in chartControl1.Series)
            {
                barListItem1.Strings.Add(series.Name);
            }

            setAxisXSettings((chartControl1.Diagram as XYDiagram).AxisX);

            System.Collections.IComparer comparer = new DateComparer();

            XYDiagram diagram = chartControl1.Diagram as XYDiagram;
            diagram.AxisX.QualitativeScaleComparer = comparer;

            diagram.EnableAxisXScrolling = true;
            diagram.EnableAxisXZooming = true;
            diagram.EnableAxisYScrolling = true;
            diagram.EnableAxisYZooming = true;

            chartControl1.CustomPaint += OnChartCustomPaint;
        }

        class DateComparer : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                try
                {
                    return DateTime.Parse(x as string).CompareTo(DateTime.Parse(y as string));
                }
                catch
                {
                    return -1;
                }
            }
        }

        public void setAxisXSettings(AxisXBase axisX)
        {
            axisX.Label.Font = new System.Drawing.Font("Segoe UI", 8F);
            axisX.Label.Angle = 30;

            axisX.Title.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            axisX.Title.Visibility = DevExpress.Utils.DefaultBoolean.True;

            axisX.GridLines.Visible = true;
            axisX.Alignment = AxisAlignment.Near;

            axisX.ScaleBreakOptions.SizeInPixels = 8;
            axisX.ScaleBreakOptions.Style = ScaleBreakStyle.Straight;
            axisX.AutoScaleBreaks.MaxCount = 2;

            axisX.Interlaced = true;
            axisX.InterlacedColor = System.Drawing.Color.FromArgb(60, System.Drawing.Color.Gray);
            axisX.InterlacedFillStyle.FillMode = DevExpress.XtraCharts.FillMode.Gradient;

            axisX.WholeRange.SideMarginSizeUnit = SideMarginSizeUnit.AxisRangePercentage;
            axisX.WholeRange.SideMarginsValue = 2;
        }

        private void _GenerateSeries()
        {
            DataTable table = chartControl1.DataSource as DataTable;
            SecondaryAxisY axisY = new SecondaryAxisY();

            Series stepSeries = new Series("Step", ViewType.Point);
            stepSeries.ArgumentDataMember = "Step_DateTime";
            stepSeries.ValueDataMembers.AddRange("Step_Value");
            chartControl1.Series.Add(stepSeries);
            (chartControl1.Diagram as XYDiagram).SecondaryAxesY.Add(axisY);
            (stepSeries.View as PointSeriesView).AxisY = axisY;

            string[] stepArgument = new string[table.Rows.Count * table.Columns.Count];
            double[] stepValues = new double[table.Rows.Count * table.Columns.Count];
            int i = 0;

            foreach (DataColumn column in table.Columns)
            {
                string name = column.ColumnName;

                if (name.Contains("PC2_TURBOBACK_PRS_timestamp") || name.Contains("PC2_ESC_VOLTAGE_timestamp") || name.Contains("PC2_PRESSURE_timestamp"))
                {
                    string[] fields = name.Split('_');
                    string[] variableStrings = fields.Skip(1).Take(fields.Length - 2).ToArray();
                    string step = fields[0];

                    string variable = variableStrings[0];
                    for (int j = 1; j < variableStrings.Length; j++)
                    {
                        variable += "_" + variableStrings[j];
                    }

                    string valueName = step + "_" + variable + "_value";

                    stepArgument[i] = table.Rows[0].Field<string>(name);
                    stepValues[i] = double.Parse(step);
                    i++;

                    //SeriesPoint point = new SeriesPoint(table.Rows[0].Field<string>(name), new object[] { 0, step });
                    //stepSeries.Points.Add(point);

                    Series series = new Series(name, ViewType.ScatterLine);
                    series.ArgumentDataMember = name;
                    series.ValueDataMembers.AddRange(new string[] { valueName });

                    chartControl1.Series.Add(series);
                }
            }

            DataTable stepTable = new DataTable("Step data");
            stepTable.Columns.Add("Step_DateTime");
            stepTable.Columns.Add("Step_Value", typeof(double));

            for (int j = 0; j < stepArgument.Length; j++)
            {
                DataRow row = stepTable.NewRow();
                row.ItemArray = new object[] { stepArgument[j], stepValues[j] };
                stepTable.Rows.Add(row);
            }

            stepSeries.BindToData(stepTable, "Step_DateTime", "Step_Value");
        }

        private DataTable _GenerateChart(JObject data)
        {
            DataTable table = new DataTable();

            var arr = data["process"]["data"] as JArray;

            foreach (JObject obj in arr)
            {
                string step = obj["step"].ToString();

                JArray subobj = obj["parameters"] as JArray;

                foreach (JObject fieldobj in subobj)
                {
                    string name = fieldobj["name"].ToString();

                    DataColumn timestampColumn = new DataColumn(step + "_" + name + "_timestamp", typeof(string));
                    DataColumn valueColumn = new DataColumn(step + "_" + name + "_value", typeof(double));

                    table.Columns.AddRange(new DataColumn[] {
                        timestampColumn, valueColumn
                    });

                    JToken[] timestamps = fieldobj["time"].ToArray();
                    JToken[] values = fieldobj["value"].ToArray();

                    int row = 0;
                    for (int i = 0; i < timestamps.Length; i++)
                    {
                        DataRow dataRow = null;

                        if (row >= table.Rows.Count)
                        {
                            dataRow = table.NewRow();
                            table.Rows.Add(dataRow);
                        }
                        else
                            dataRow = table.Rows[row];

                        dataRow.SetField(timestampColumn, (string)timestamps[i]);
                        dataRow.SetField(valueColumn, (double)values[i]);

                        row++;
                    }
                }
            }

            return table;
        }

        int[] x = null;
        int[] y = null;

        private void OnChartCustomPaint(object sender, CustomPaintEventArgs e)
        {
            DXCustomPaintEventArgs dxArgs = e as DXCustomPaintEventArgs;
            if (dxArgs == null) return;

            Pen pen = new Pen(new SolidBrush(Color.DarkBlue));
            Brush fillBrush = new SolidBrush(Color.Black);

            Font font = new Font("Segoe UI", 11);
            Brush stringBrush = new SolidBrush(Color.White);

            XYDiagram diagram = chartControl1.Diagram as XYDiagram;
            Series series = chartControl1.Series["Step"];

            Rectangle clientRect = chartControl1.DisplayRectangle;

            int size = series.Points.Count;
            x = new int[size];
            y = new int[size];

            int multiplier = 0;

            int increaseYStep = 15;

            int stepVal = 3;
            int rectWidth = 20;
            int rectHeight = 13;

            int textXAlign = 8;
            int textYAlign = 4;
            int textExtraDigitAlign = 4;
            int bottomPadding = 200;

            for (int i = 0; i < size; i = i + stepVal)
            {
                SeriesPoint point = series.Points[i];

                ControlCoordinates coordinates = null;
                try
                {
                    coordinates = diagram.DiagramToPoint(point.Argument, 0);
                }
                catch (Exception ex)
                { }

                if(coordinates != null)
                {
                    Point coordPoint = new Point(coordinates.Point.X - (rectWidth / 2), clientRect.Height - bottomPadding);

                    Rectangle rect = new Rectangle(coordPoint, new Size(rectWidth, rectHeight));

                    double value = point.Values[0];

                    x[i] = value > 9 ? coordinates.Point.X - textXAlign : coordinates.Point.X - textXAlign + textExtraDigitAlign;

                    if (i > stepVal - 1)
                    {
                        int val = Math.Abs(x[i] - x[i - stepVal]);

                        if (val < rectWidth)
                        {
                            multiplier++;
                            y[i] = clientRect.Height - bottomPadding - textYAlign - increaseYStep * multiplier; // Add code for collision handling
                            rect = new Rectangle(rect.X, rect.Y - increaseYStep * multiplier, rect.Width, rect.Height);
                        }
                        else
                        {
                            multiplier = 0;
                            y[i] = clientRect.Height - bottomPadding - textYAlign;
                        }
                    }
                    else
                    {
                        y[i] = clientRect.Height - bottomPadding - textYAlign;
                    }

                    dxArgs.Cache.FillEllipse(fillBrush, rect);
                    dxArgs.Cache.DrawEllipse(pen, rect);
                    dxArgs.Cache.DrawString(value.ToString(), font, stringBrush, new Point(x[i], y[i]));
                }
            }
        }

        private void barListItem1_ListItemClick(object sender, DevExpress.XtraBars.ListItemClickEventArgs e)
        {
            foreach(Series series in chartControl1.Series)
            {
                if((e.Item as BarListItem).Strings[e.Index].Equals(series.Name))
                {
                    series.Visible = true;
                }
                else
                {
                    series.Visible = false;
                }
            }
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            foreach (Series series in chartControl1.Series)
            {
                series.Visible = true;
            }

            XYDiagram diagram = chartControl1.Diagram as XYDiagram;
            diagram.AxisX.VisualRange.Auto = true;
            diagram.AxisY.VisualRange.Auto = true;
        }
    }
}


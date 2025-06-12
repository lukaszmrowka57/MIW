using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace IrisKnn
{
    public delegate double Metric(double[] a, double[] b, double param);

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MainForm());
        }
    }

    public class IrisSample
    {
        public double[] Attributes;
        public int Class;

        public IrisSample(double[] attrs, int cls)
        {
            Attributes = attrs;
            Class = cls;
        }
    }

    public class MainForm : Form
    {
        Button btnLoad, btnRun;
        TextBox txtPath, txtK, txtMinkowskiP, txtLogParam;
        ComboBox cmbMetric;
        Label lblK, lblMetric, lblP, lblLog;
        RichTextBox txtResults;
        List<IrisSample> samples = new List<IrisSample>();
        List<IrisSample> normSamples = new List<IrisSample>();
        Metric selectedMetric;
        double metricParam = 2;

        public MainForm()
        {
            this.Text = "KNN – Iris – 1 vs Reszta";
            this.Width = 1850;
            this.Height = 1000;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var bigFont = new System.Drawing.Font("Segoe UI", 20);

            txtPath = new TextBox() { Left = 10, Top = 10, Width = 1200, Height = 50, Font = bigFont, Text = "iris.txt" };

            Button btnPickFile = new Button()
            {
                Left = 1220,
                Top = 10,
                Width = 200,
                Height = 50,
                Font = bigFont,
                Text = "Wybierz plik..."
            };
            btnPickFile.Click += (s, e) =>
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Wybierz plik iris";
                dlg.Filter = "Pliki txt (*.txt)|*.txt|Wszystkie pliki|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtPath.Text = dlg.FileName;
            };

            btnLoad = new Button() { Left = 1440, Top = 10, Width = 200, Height = 50, Font = bigFont, Text = "Wczytaj" };
            btnLoad.Click += (s, e) => { LoadSamples(txtPath.Text); };

            lblK = new Label() { Left = 10, Top = 80, Width = 80, Height = 50, Font = bigFont, Text = "k:" };
            txtK = new TextBox() { Left = 100, Top = 80, Width = 80, Height = 50, Font = bigFont, Text = "3" };

            lblMetric = new Label() { Left = 220, Top = 80, Width = 200, Height = 50, Font = bigFont, Text = "Metryka:" };
            cmbMetric = new ComboBox() { Left = 430, Top = 80, Width = 400, Height = 50, Font = bigFont, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbMetric.Items.AddRange(new string[] { "Manhattan", "Czebyszew", "Minkowski", "Logarytmiczna" });
            cmbMetric.SelectedIndex = 0;
            cmbMetric.SelectedIndexChanged += (s, e) => UpdateMetricFields();

            lblP = new Label() { Left = 870, Top = 80, Width = 300, Height = 50, Font = bigFont, Text = "p (Minkowski):" };
            txtMinkowskiP = new TextBox() { Left = 1190, Top = 80, Width = 100, Height = 50, Font = bigFont, Text = "2" };
            lblP.Visible = false;
            txtMinkowskiP.Visible = false;

            lblLog = new Label() { Left = 1300, Top = 80, Width = 350, Height = 50, Font = bigFont, Text = "log(a+b) param:" };
            txtLogParam = new TextBox() { Left = 1680, Top = 80, Width = 100, Height = 50, Font = bigFont, Text = "1" };
            lblLog.Visible = false;
            txtLogParam.Visible = false;

            btnRun = new Button() { Left = 10, Top = 150, Width = 300, Height = 60, Font = bigFont, Text = "Start" };
            btnRun.Click += (s, e) => RunExperiment();

            txtResults = new RichTextBox() { Left = 10, Top = 230, Width = 1800, Height = 700, ReadOnly = true, Font = new System.Drawing.Font("Consolas", 18) };

            Controls.AddRange(new Control[] { txtPath, btnPickFile, btnLoad, lblK, txtK, lblMetric, cmbMetric, lblP, txtMinkowskiP, lblLog, txtLogParam, btnRun, txtResults });
        }

        
        void UpdateMetricFields()
        {
            lblP.Visible = cmbMetric.SelectedItem.ToString() == "Minkowski";
            txtMinkowskiP.Visible = lblP.Visible;
            lblLog.Visible = cmbMetric.SelectedItem.ToString() == "Logarytmiczna";
            txtLogParam.Visible = lblLog.Visible;
        }

        void LoadSamples(string path)
        {
            samples.Clear();
            if (!File.Exists(path))
            {
                MessageBox.Show("Nie znaleziono pliku.");
                return;
            }
            using (var sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim();
                    if (line == "") continue;
                    var split = line.Split(new[] { '\t', ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length < 2) continue;
                    double[] attrs = new double[split.Length - 1];
                    for (int i = 0; i < attrs.Length; i++)
                        attrs[i] = double.Parse(split[i].Replace(",", "."), CultureInfo.InvariantCulture);
                    int cls = int.Parse(split.Last());
                    samples.Add(new IrisSample(attrs, cls));
                }
            }
            Normalize();
            txtResults.Text = $"Wczytano {samples.Count} próbek. Dane znormalizowane.";
        }

        void Normalize()
        {
            if (samples.Count == 0) return;
            int dim = samples[0].Attributes.Length;
            double[] min = new double[dim];
            double[] max = new double[dim];
            for (int i = 0; i < dim; i++)
            {
                min[i] = samples.Min(s => s.Attributes[i]);
                max[i] = samples.Max(s => s.Attributes[i]);
            }
            normSamples = samples.Select(s =>
            {
                double[] norm = new double[dim];
                for (int i = 0; i < dim; i++)
                    norm[i] = (max[i] == min[i]) ? 0 : (s.Attributes[i] - min[i]) / (max[i] - min[i]);
                return new IrisSample(norm, s.Class);
            }).ToList();
        }
        void RunExperiment()
        {
            if (normSamples.Count == 0)
            {
                txtResults.Text = "Brak danych! Najpierw wczytaj plik iris.txt.";
                return;
            }

            int k = 3;
            if (!int.TryParse(txtK.Text, out k) || k < 1)
            {
                txtResults.Text = "Niepoprawna wartoœæ k!";
                return;
            }
            string m = cmbMetric.SelectedItem.ToString();
            if (m == "Manhattan")
            {
                selectedMetric = MetricManhattan;
                metricParam = 0;
            }
            else if (m == "Czebyszew")
            {
                selectedMetric = MetricCzebyszew;
                metricParam = 0;
            }
            else if (m == "Minkowski")
            {
                selectedMetric = MetricMinkowski;
                double p;
                if (!double.TryParse(txtMinkowskiP.Text, out p) || p <= 0)
                {
                    txtResults.Text = "Niepoprawne p (Minkowski)!";
                    return;
                }
                metricParam = p;
            }
            else if (m == "Logarytmiczna")
            {
                selectedMetric = MetricLogarithmic;
                double a;
                if (!double.TryParse(txtLogParam.Text, out a) || a <= 0)
                {
                    txtResults.Text = "Niepoprawny parametr log!";
                    return;
                }
                metricParam = a;
            }
            else
            {
                txtResults.Text = "Nie wybrano metryki!";
                return;
            }

            int total = normSamples.Count;
            int correct = 0;
            int wrong = 0;
            int tie = 0;
            txtResults.Text = $"Start eksperymentu k={k}, metryka: {m}, param={metricParam}\n";
            for (int i = 0; i < total; i++)
            {
                var testSample = normSamples[i];
                var trainSamples = normSamples.Where((s, idx) => idx != i).ToList();

                var neighbors = trainSamples
                    .Select(s => new { Sample = s, Dist = selectedMetric(testSample.Attributes, s.Attributes, metricParam) })
                    .OrderBy(x => x.Dist)
                    .Take(k)
                    .ToList();

                var groups = neighbors.GroupBy(n => n.Sample.Class)
                    .Select(g => new { Class = g.Key, Count = g.Count() })
                    .OrderByDescending(g => g.Count)
                    .ToList();

                int predicted = -1;
                bool tieFlag = false;
                if (groups.Count > 1 && groups[0].Count == groups[1].Count)
                {
                    tie++;
                    tieFlag = true;
                }
                else
                {
                    predicted = groups[0].Class;
                }

                if (tieFlag)
                    txtResults.AppendText($"[{i + 1}] Remis. Prawid³owa klasa: {testSample.Class}\n");
                else if (predicted == testSample.Class)
                {
                    correct++;
                }
                else
                {
                    wrong++;
                    txtResults.AppendText($"[{i + 1}] B³¹d: pred: {predicted} | oczekiwana: {testSample.Class}\n");
                }
            }
            double acc = 100.0 * correct / total;
            txtResults.AppendText($"\nSklasyfikowano prawid³owo: {correct}/{total} ({acc:F2}%)\n");
            txtResults.AppendText($"B³êdnych: {wrong}\nRemisów: {tie}\n");
        }

        double MetricManhattan(double[] a, double[] b, double param)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += Math.Abs(a[i] - b[i]);
            return sum;
        }

        double MetricCzebyszew(double[] a, double[] b, double param)
        {
            double max = 0;
            for (int i = 0; i < a.Length; i++)
            {
                double d = Math.Abs(a[i] - b[i]);
                if (d > max) max = d;
            }
            return max;
        }

        double MetricMinkowski(double[] a, double[] b, double p)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += Math.Pow(Math.Abs(a[i] - b[i]), p);
            return Math.Pow(sum, 1.0 / p);
        }

        double MetricLogarithmic(double[] a, double[] b, double alpha)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
                sum += Math.Log(alpha + Math.Abs(a[i] - b[i]));
            return sum;
        }
    }
}

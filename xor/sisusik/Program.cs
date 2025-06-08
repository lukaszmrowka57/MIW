using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OneFileGA_Zad2
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private Button btnStart;
        private TextBox txtOutput;
        private Label lblBest;

        public MainForm()
        {
            this.Text = "Zadanie 2 - Dopasowanie funkcji";
            this.Width = 600;
            this.Height = 500;

            btnStart = new Button { Text = "Start", Top = 10, Left = 10, Width = 100 };
            lblBest = new Label { Text = "Najlepszy: -", Top = 15, Left = 130, Width = 300 };
            txtOutput = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Top = 50, Left = 10, Width = 560, Height = 380 };

            btnStart.Click += BtnStart_Click;

            this.Controls.Add(btnStart);
            this.Controls.Add(lblBest);
            this.Controls.Add(txtOutput);
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
            var ga = new GeneticAlgorithm
            {
                LogOutput = msg => txtOutput.AppendText(msg + Environment.NewLine),
                UpdateBestFitness = v => lblBest.Text = $"Najlepszy: {v:F6}"
            };
            ga.Run();
        }
    }

    public class Individual
    {
        public bool[] Chromosomes;
        public double Fitness;

        public Individual(int length)
        {
            Chromosomes = new bool[length];
        }

        public Individual(bool[] chromosomes)
        {
            Chromosomes = chromosomes.ToArray();
        }
    }

    public class GeneticAlgorithm
    {
        private Random rand = new Random();
        private int populationSize = 13;
        private int bitsPerParam = 5;
        private int paramCount = 3;
        private int totalBits => bitsPerParam * paramCount;
        private double minValue = 0;
        private double maxValue = 3;
        private int iterations = 100;
        private int tournamentSize = 3;

        private List<(double x, double y)> samples;
        private List<Individual> population = new List<Individual>();

        public Action<string> LogOutput;
        public Action<double> UpdateBestFitness;

        public GeneticAlgorithm()
        {
            samples = LoadSampleData();
        }

        public void Run()
        {
            population = GenerateInitialPopulation();

            for (int i = 0; i <= iterations; i++)
            {
                foreach (var ind in population)
                    ind.Fitness = Evaluate(ind);

                double avg = population.Average(p => p.Fitness);
                double best = population.Min(p => p.Fitness);
                UpdateBestFitness?.Invoke(best);
                LogOutput?.Invoke($"Iteracja {i}: Œrednia={avg:F6}, Najlepszy={best:F6}");

                if (i == iterations) break;

                List<Individual> newPop = new List<Individual>();

                // 4x selekcja + krzy¿owanie
                for (int j = 0; j < 4; j++)
                {
                    var p1 = TournamentSelection();
                    var p2 = TournamentSelection();
                    var (c1, _) = Crossover(p1, p2);
                    newPop.Add(c1);
                }

                // 4x selekcja + mutacja
                for (int j = 0; j < 4; j++)
                {
                    var p = TournamentSelection();
                    newPop.Add(Mutate(p));
                }

                // 4x selekcja + krzy¿owanie + mutacja
                for (int j = 0; j < 4; j++)
                {
                    var p1 = TournamentSelection();
                    var p2 = TournamentSelection();
                    var (c1, _) = Crossover(p1, p2);
                    newPop.Add(Mutate(c1));
                }

                newPop.Add(GetBestIndividual()); // elita
                population = newPop.Take(populationSize).ToList();
            }
        }

        private List<(double x, double y)> LoadSampleData() => new()
        {
            (-1.0, 0.59554), (-0.8, 0.58813), (-0.6, 0.64181), (-0.4, 0.68587),
            (-0.2, 0.44783), (0.0, 0.40836), (0.2, 0.38241), (0.4, -0.05933),
            (0.6, -0.12478), (0.8, -0.36847), (1.0, -0.39935), (1.2, -0.50881),
            (1.4, -0.63435), (1.6, -0.59979), (1.8, -0.64107), (2.0, -0.51808),
            (2.2, -0.38127), (2.4, -0.12349), (2.6, -0.09624), (2.8, 0.27893),
            (3.0, 0.48965), (3.2, 0.33089), (3.4, 0.70615), (3.6, 0.53342),
            (3.8, 0.43321), (4.0, 0.64790), (4.2, 0.48834), (4.4, 0.18440),
            (4.6, -0.02389), (4.8, -0.10261), (5.0, -0.33594), (5.2, -0.35101),
            (5.4, -0.62027), (5.6, -0.55719), (5.8, -0.66377), (6.0, -0.62740)
        };

        private List<Individual> GenerateInitialPopulation()
        {
            var pop = new List<Individual>();
            for (int i = 0; i < populationSize; i++)
            {
                var ind = new Individual(totalBits);
                for (int j = 0; j < totalBits; j++)
                    ind.Chromosomes[j] = rand.Next(2) == 1;
                pop.Add(ind);
            }
            return pop;
        }

        private double Evaluate(Individual ind)
        {
            var (a, b, c) = Decode(ind.Chromosomes);
            double sumSq = 0;
            foreach (var (x, yTrue) in samples)
            {
                double yPred = Math.Sin(x * a + b) * Math.Cos(x * c);
                sumSq += Math.Pow(yTrue - yPred, 2);
            }
            return sumSq;
        }

        private (double, double, double) Decode(bool[] chrom)
        {
            int maxVal = (1 << bitsPerParam) - 1;
            int[] vals = new int[paramCount];
            for (int p = 0; p < paramCount; p++)
            {
                int val = 0;
                for (int i = 0; i < bitsPerParam; i++)
                    if (chrom[p * bitsPerParam + i])
                        val |= 1 << (bitsPerParam - 1 - i);
                vals[p] = val;
            }
            return (
                minValue + vals[0] * (maxValue - minValue) / maxVal,
                minValue + vals[1] * (maxValue - minValue) / maxVal,
                minValue + vals[2] * (maxValue - minValue) / maxVal
            );
        }

        private Individual TournamentSelection()
        {
            var selected = new List<Individual>();
            for (int i = 0; i < tournamentSize; i++)
                selected.Add(population[rand.Next(population.Count)]);
            return selected.OrderBy(x => x.Fitness).First();
        }

        private Individual Mutate(Individual original)
        {
            var chrom = original.Chromosomes.ToArray();
            int idx = rand.Next(chrom.Length);
            chrom[idx] = !chrom[idx];
            return new Individual(chrom);
        }

        private (Individual, Individual) Crossover(Individual p1, Individual p2)
        {
            var len = p1.Chromosomes.Length;
            var cut = rand.Next(1, len - 1);
            bool[] c1 = new bool[len];
            bool[] c2 = new bool[len];
            for (int i = 0; i < len; i++)
            {
                c1[i] = i < cut ? p1.Chromosomes[i] : p2.Chromosomes[i];
                c2[i] = i < cut ? p2.Chromosomes[i] : p1.Chromosomes[i];
            }
            return (new Individual(c1), new Individual(c2));
        }

        private Individual GetBestIndividual()
        {
            return population.OrderBy(p => p.Fitness).First();
        }
    }
}

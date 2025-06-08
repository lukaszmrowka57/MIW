using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OneFileGA_Zad3
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
            this.Text = "Zadanie 3 - XOR optymalizacja wag";
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
        private int bitsPerParam = 8;
        private int paramCount = 9; // 3 neurony, po 3 wagi
        private int totalBits => bitsPerParam * paramCount;
        private double minValue = -10;
        private double maxValue = 10;
        private int iterations = 100;
        private int tournamentSize = 3;

        private List<Individual> population = new List<Individual>();

        public Action<string> LogOutput;
        public Action<double> UpdateBestFitness;

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

                for (int j = 0; j < 4; j++)
                {
                    var p1 = TournamentSelection();
                    var p2 = TournamentSelection();
                    var (c1, _) = Crossover(p1, p2);
                    newPop.Add(c1);
                }

                for (int j = 0; j < 4; j++)
                {
                    var p = TournamentSelection();
                    newPop.Add(Mutate(p));
                }

                for (int j = 0; j < 4; j++)
                {
                    var p1 = TournamentSelection();
                    var p2 = TournamentSelection();
                    var (c1, _) = Crossover(p1, p2);
                    newPop.Add(Mutate(c1));
                }

                newPop.Add(GetBestIndividual());
                population = newPop.Take(populationSize).ToList();
            }
        }

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
            var weights = Decode(ind.Chromosomes);

            var data = new (double[] input, double target)[]
            {
                (new double[] {0, 0, 1}, 0),
                (new double[] {0, 1, 1}, 1),
                (new double[] {1, 0, 1}, 1),
                (new double[] {1, 1, 1}, 0)
            };

            double sumSq = 0;
            foreach (var (input, target) in data)
            {
                double sum = 0;
                for (int i = 0; i < 3; i++)
                {
                    double dot = 0;
                    for (int j = 0; j < 3; j++)
                        dot += input[j] * weights[i * 3 + j];
                    sum += Sigmoid(dot);
                }
                double output = sum / 3.0; // œrednia z neuronów
                sumSq += Math.Pow(output - target, 2);
            }
            return sumSq;
        }

        private double Sigmoid(double x) => 1.0 / (1.0 + Math.Exp(-x));

        private double[] Decode(bool[] chrom)
        {
            int maxVal = (1 << bitsPerParam) - 1;
            double[] result = new double[paramCount];
            for (int p = 0; p < paramCount; p++)
            {
                int val = 0;
                for (int i = 0; i < bitsPerParam; i++)
                    if (chrom[p * bitsPerParam + i])
                        val |= 1 << (bitsPerParam - 1 - i);
                result[p] = minValue + val * (maxValue - minValue) / maxVal;
            }
            return result;
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
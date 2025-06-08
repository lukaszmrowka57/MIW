using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OneFileGA
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
            this.Text = "Zmutowany Dywanik";
            this.Width = 520;
            this.Height = 420;

            btnStart = new Button { Text = "Start", Top = 10, Left = 10, Width = 100 };
            lblBest = new Label { Text = "Najlepszy: -", Top = 15, Left = 130, Width = 200 };
            txtOutput = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Top = 50, Left = 10, Width = 480, Height = 300 };

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
                UpdateBestFitness = v => lblBest.Text = $"Najlepszy: {v:F4}"
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
        private int populationSize = 11;
        private int bitsPerParam = 5;
        private int totalBits => bitsPerParam * 2;
        private double minValue = 0;
        private double maxValue = 100;
        private int iterations = 50;
        private int tournamentSize => Math.Max(2, populationSize / 10);

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
                double best = population.Max(p => p.Fitness);
                UpdateBestFitness?.Invoke(best);
                LogOutput?.Invoke($"Iteracja {i}: Œrednia={avg:F4}, Najlepszy={best:F4}");

                if (i == iterations) break;

                List<Individual> newPop = new List<Individual>();

                for (int j = 0; j < populationSize - 1; j++)
                {
                    Individual selected = TournamentSelection();
                    Individual mutated = Mutate(selected);
                    newPop.Add(mutated);
                }

                newPop.Add(GetBestIndividual());
                population = newPop;
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
            (double x1, double x2) = Decode(ind.Chromosomes);
            return Math.Sin(x1 * 0.05) + Math.Sin(x2 * 0.05)
                 + 0.4 * Math.Sin(x1 * 0.15) * Math.Sin(x2 * 0.15);
        }

        private (double, double) Decode(bool[] chrom)
        {
            int maxVal = (1 << bitsPerParam) - 1;
            int val1 = 0, val2 = 0;
            for (int i = 0; i < bitsPerParam; i++)
            {
                if (chrom[i]) val1 |= 1 << (bitsPerParam - 1 - i);
                if (chrom[i + bitsPerParam]) val2 |= 1 << (bitsPerParam - 1 - i);
            }
            double x1 = minValue + val1 * (maxValue - minValue) / maxVal;
            double x2 = minValue + val2 * (maxValue - minValue) / maxVal;
            return (x1, x2);
        }

        private Individual TournamentSelection()
        {
            var selected = new List<Individual>();
            for (int i = 0; i < tournamentSize; i++)
                selected.Add(population[rand.Next(population.Count)]);
            return selected.OrderByDescending(x => x.Fitness).First();
        }

        private Individual Mutate(Individual original)
        {
            var chrom = original.Chromosomes.ToArray();
            int idx = rand.Next(chrom.Length);
            chrom[idx] = !chrom[idx];
            return new Individual(chrom);
        }

        private Individual GetBestIndividual()
        {
            return population.OrderByDescending(p => p.Fitness).First();
        }
    }
}

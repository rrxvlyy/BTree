using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static BTree.Program;

namespace BTree
{

    internal class Program
    {
        public class OperationResult
        {
            public long Operations { get; set; }
            public long TimeTicks { get; set; }
        }

        public class BStarNode
        {
            public List<int> Keys;
            public List<BStarNode> Children;
            public bool IsLeaf;

            public BStarNode(bool isLeaf)
            {
                IsLeaf = isLeaf;
                Keys = new List<int>();
                Children = new List<BStarNode>();
            }
        }

        public class BStarTree
        {
            // Минимальная степень
            private readonly int t;

            // Максимум ключей = 2t - 1
            private readonly int maxKeys;

            public BStarNode Root;

            public BStarTree(int degree)
            {
                t = degree;
                maxKeys = 2 * t - 1;
                Root = new BStarNode(true);
            }

            public bool Search(int key, out OperationResult result)
            {
                long ops = 0;
                Stopwatch sw = Stopwatch.StartNew();

                bool found = SearchInternal(Root, key, ref ops);

                sw.Stop();

                result = new OperationResult
                {
                    Operations = ops,
                    TimeTicks = sw.ElapsedTicks
                };

                return found;
            }

            private bool SearchInternal(BStarNode node, int key, ref long ops)
            {
                int i = 0;

                while (i < node.Keys.Count && key > node.Keys[i])
                {
                    ops++;
                    i++;
                }
                if (i < node.Keys.Count)
                    ops++;

                if (i < node.Keys.Count && node.Keys[i] == key)
                {
                    return true;
                }

                if (node.IsLeaf)
                {
                    return false;
                }

                return SearchInternal(node.Children[i], key, ref ops);
            }

            public OperationResult Insert(int key)
            {
                long ops = 0;
                Stopwatch sw = Stopwatch.StartNew();

                if (Root.Keys.Count == maxKeys)
                {
                    BStarNode newRoot = new BStarNode(false);
                    newRoot.Children.Add(Root);

                    SplitChild(newRoot, 0, ref ops);

                    Root = newRoot;
                }

                InsertNonFull(Root, key, ref ops);

                sw.Stop();

                return new OperationResult
                {
                    Operations = ops,
                    TimeTicks = sw.ElapsedTicks
                };
            }

            private void InsertNonFull(BStarNode node, int key, ref long ops)
            {
                int i = node.Keys.Count - 1;

                if (node.IsLeaf)
                {
                    node.Keys.Add(0);

                    while (i >= 0 && key < node.Keys[i])
                    {
                        ops++;
                        node.Keys[i + 1] = node.Keys[i];
                        i--;
                    }

                    node.Keys[i + 1] = key;
                    ops++;
                }
                else
                {
                    while (i >= 0 && key < node.Keys[i])
                    {
                        ops++;
                        i--;
                    }

                    i++;

                    if (node.Children[i].Keys.Count == maxKeys)
                    {
                        SplitChild(node, i, ref ops);

                        if (key > node.Keys[i])
                        {
                            i++;
                        }
                    }

                    InsertNonFull(node.Children[i], key, ref ops);
                }
            }

            private void SplitChild(BStarNode parent, int index, ref long ops)
            {
                BStarNode fullNode = parent.Children[index];
                BStarNode newNode = new BStarNode(fullNode.IsLeaf);

                int middle = t - 1;

                // Перенос правой половины
                for (int j = 0; j < t - 1; j++)
                {
                    newNode.Keys.Add(fullNode.Keys[j + t]);
                    ops++;
                }

                if (!fullNode.IsLeaf)
                {
                    for (int j = 0; j < t; j++)
                    {
                        newNode.Children.Add(fullNode.Children[j + t]);
                        ops++;
                    }
                }

                int middleKey = fullNode.Keys[middle];

                fullNode.Keys.RemoveRange(middle, fullNode.Keys.Count - middle);

                if (!fullNode.IsLeaf)
                {
                    fullNode.Children.RemoveRange(t, fullNode.Children.Count - t);
                }

                parent.Children.Insert(index + 1, newNode);
                parent.Keys.Insert(index, middleKey);

                ops += 5;
            }

            public OperationResult Delete(int key)
            {
                long ops = 0;
                Stopwatch sw = Stopwatch.StartNew();

                DeleteInternal(Root, key, ref ops);

                if (Root.Keys.Count == 0 && !Root.IsLeaf)
                {
                    Root = Root.Children[0];
                }

                sw.Stop();

                return new OperationResult
                {
                    Operations = ops,
                    TimeTicks = sw.ElapsedTicks
                };
            }

            private void DeleteInternal(BStarNode node, int key, ref long ops)
            {
                int idx = FindKey(node, key, ref ops);

                if (idx < node.Keys.Count && node.Keys[idx] == key)
                {
                    if (node.IsLeaf)
                    {
                        node.Keys.RemoveAt(idx);
                        ops++;
                    }
                    else
                    {
                        int predecessor = GetPredecessor(node, idx, ref ops);
                        node.Keys[idx] = predecessor;
                        DeleteInternal(node.Children[idx], predecessor, ref ops);
                    }
                }
                else
                {
                    if (node.IsLeaf)
                    {
                        return;
                    }

                    DeleteInternal(node.Children[idx], key, ref ops);
                }
            }

            private int FindKey(BStarNode node, int key, ref long ops)
            {
                int idx = 0;

                while (idx < node.Keys.Count && node.Keys[idx] < key)
                {
                    ops++;
                    idx++;
                }

                return idx;
            }

            private int GetPredecessor(BStarNode node, int idx, ref long ops)
            {
                BStarNode current = node.Children[idx];

                while (!current.IsLeaf)
                {
                    current = current.Children[current.Children.Count - 1];
                    ops++;
                }

                return current.Keys[current.Keys.Count - 1];
            }
        }


        static void Main(string[] args)
        {
            const int COUNT = 10000;

            Random random = new Random();

            int[] values = new int[COUNT];

            for (int i = 0; i < COUNT; i++)
            {
                values[i] = random.Next(0, 1000000);
            }

            BStarTree tree = new BStarTree(4);

            List<OperationResult> insertResults = new List<OperationResult>();
            List<OperationResult> searchResults = new List<OperationResult>();
            List<OperationResult> deleteResults = new List<OperationResult>();

            foreach (int value in values)
            {
                insertResults.Add(tree.Insert(value));
            }

            for (int i = 0; i < 100; i++)
            {
                int value = values[random.Next(values.Length)];

                tree.Search(value, out OperationResult result);

                searchResults.Add(result);
            }

            for (int i = 0; i < 1000; i++)
            {
                int value = values[random.Next(values.Length)];

                deleteResults.Add(tree.Delete(value));
            }


            double avgInsertOps = insertResults.Average(r => r.Operations);
            double avgInsertTicks = insertResults.Average(r => r.TimeTicks);

            double avgSearchOps = searchResults.Average(r => r.Operations);
            double avgSearchTicks = searchResults.Average(r => r.TimeTicks);

            double avgDeleteOps = deleteResults.Average(r => r.Operations);
            double avgDeleteTicks = deleteResults.Average(r => r.TimeTicks);

            // перевод тиков в миллисекунды
            double tickMs = 1000.0 / Stopwatch.Frequency;

            Console.WriteLine("- Средние значения -");

            Console.WriteLine(
                $"Вставка: {avgInsertTicks * tickMs:F8} ms, операции: {avgInsertOps:F0}");

            Console.WriteLine(
                $"Поиск: {avgSearchTicks * tickMs:F8} ms, операции: {avgSearchOps:F0}");

            Console.WriteLine(
                $"Удаление: {avgDeleteTicks * tickMs:F16} ms, операции: {avgDeleteOps:F0}");
        }

        static void PrintAverage(List<OperationResult> results)
        {
            double avgOps = results.Average(r => r.Operations);
            double avgTicks = results.Average(r => r.TimeTicks);

            Console.WriteLine($"Average operations: {avgOps:F2}");
            Console.WriteLine($"Average ticks: {avgTicks:F2}");
        }
    }
}
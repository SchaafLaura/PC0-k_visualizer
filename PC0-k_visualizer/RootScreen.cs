using PC0;
using SadConsole;
using System.Net.Http.Headers;

namespace PC0_k_visualizer.Scenes
{
    internal class RootScreen : ScreenObject
    {
        private ScreenSurface _mainSurface;
        private SudokuCellSurface[,] cellSurfaces;
        List<int>[] domains;
        Solver<int> solver;
        public RootScreen()
        {
            // Create a surface that's the same size as the screen.
            _mainSurface = new ScreenSurface(GameSettings.GAME_WIDTH, GameSettings.GAME_HEIGHT);

            var boundingBox = new Rectangle(
                _mainSurface.Position.X, _mainSurface.Position.Y,
                _mainSurface.Width, _mainSurface.Height);
            _mainSurface.DrawBox(boundingBox, ShapeParameters.CreateStyledBoxThick(Color.Red));

            _mainSurface.DrawLine(new Point(16, 1), new Point(16, 47), 11 * 16 + 10, Color.Red);
            _mainSurface.DrawLine(new Point(32, 1), new Point(32, 47), 11 * 16 + 10, Color.Red);

            _mainSurface.DrawLine(new Point(1, 16), new Point(47, 16), 12 * 16 + 13, Color.Red);
            _mainSurface.DrawLine(new Point(1, 32), new Point(47, 32), 12 * 16 + 13, Color.Red);

            cellSurfaces = new SudokuCellSurface[9, 9];
            Random rng = new Random();
            var xoff = 0;
            var yoff = 0;
            for (int bi = 0; bi < 3; bi++)
            {
                for (int bj = 0; bj < 3; bj++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            var x0 = (1 + bi * 15 + xoff) + i * 5;
                            var y0 = (1 + bj * 15 + yoff) + j * 5;

                            var cellSurface = new SudokuCellSurface(5, 5);
                            
                            cellSurface.Position = new Point(x0, y0);
                            /*if (rng.Next(100) < 2)
                                cellSurface.DrawDomain(new List<int>() { rng.Next(10) });
                            else
                                cellSurface.DrawDomain(new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9 });*/
                            cellSurfaces[i + bi * 3, j + bj * 3] = cellSurface;
                            Children.Add(cellSurface);
                        }
                    }
                    yoff += 1;
                }
                xoff += 1;
                yoff = 0;
            }

            domains = GetDomains();
            Dictionary<int, Func<int, bool>> unaryConstraints = new();
            Dictionary<VariableList<int>, Func<List<int>, bool>> constraints = new();
            /*var board = new int[,]
            {
                {0, 0, 0,  0, 0, 0,  0, 0, 0 },
                {6, 0, 0,  0, 3, 0,  0, 0, 4 },
                {0, 0, 2,  5, 0, 0,  0, 0, 8 },
            
                {3, 0, 0,  0, 0, 5,  8, 0, 7 },
                {7, 0, 0,  1, 0, 0,  0, 0, 0 },
                {8, 0, 0,  2, 0, 0,  0, 0, 6 },
            
                {0, 0, 6,  0, 0, 1,  0, 4, 0 },
                {0, 0, 9,  4, 0, 0,  0, 0, 5 },
                {0, 0, 0,  0, 0, 0,  1, 7, 0 },
            };*/

            var board = new int[,]
            {
                {0, 0, 0,  2, 0, 0,  0, 9, 0 },
                {9, 0, 3,  0, 6, 0,  2, 0, 7 },
                {0, 5, 4,  0, 0, 0,  8, 0, 0 },
            
                {4, 7, 0,  0, 0, 0,  0, 1, 0 },
                {0, 0, 2,  4, 0, 7,  0, 0, 0 },
                {5, 0, 0,  9, 0, 2,  0, 7, 0 },
            
                {0, 4, 0,  0, 0, 9,  7, 0, 0 },
                {0, 0, 1,  0, 0, 0,  5, 0, 0 },
                {0, 2, 6,  0, 5, 0,  0, 0, 0 },
            };


            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    if (board[x, y] == 0)
                        continue;

                    var index = XYtoI(x, y);
                    domains[index] = new List<int> { board[x, y] };
                }
            }

            for (int i = 0; i < 9; i++)
            {
                var row = GetRow(i);
                var column = GetColumn(i);
                var box = GetBox(i);
                for (int j = 0; j < 9; j++)
                {
                    var rowKey = new VariableList<int>(row);
                    var columnKey = new VariableList<int>(column);
                    var boxKey = new VariableList<int>(box);

                    constraints.Add(rowKey, ContainsAllConstraint());
                    constraints.Add(columnKey, ContainsAllConstraint());
                    constraints.Add(boxKey, ContainsAllConstraint());

                    row = row.RotateThrough();
                    column = column.RotateThrough();
                    box = box.RotateThrough();
                }
            }
            solver = new Solver<int>(1, domains, unaryConstraints, constraints);
            Children.Add(_mainSurface);
        }

        public override void Update(TimeSpan delta)
        {
            for(int i = 0; i < 2; i++)
                solver.SolveStep();
            for (int i = 0; i < 81; i++)
            {
                var xy = ItoXY(i);
                cellSurfaces[xy.y, xy.x].DrawDomain(domains[i]);
            }

        }

        List<int>[] GetDomains()
        {
            var ret = new List<int>[81];
            for (int i = 0; i < 81; i++)
            {
                ret[i] = new List<int>();
                for (int j = 1; j <= 9; j++)
                    ret[i].Add(j);
                ret[i].Shuffle();
            }
            return ret;
        }

        VariableList<int> GetBox(int i)
        {
            var ret = new VariableList<int>();
            (int x, int y) topleft = i switch
            {
                0 => (0, 0),
                1 => (3, 0),
                2 => (6, 0),
                3 => (0, 3),
                4 => (3, 3),
                5 => (6, 3),
                6 => (0, 6),
                7 => (3, 6),
                8 => (6, 6)
            };

            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                    ret.Add(XYtoI(topleft.x + dx, topleft.y + dy));
            return ret;
        }

        VariableList<int> GetColumn(int x)
        {
            var ret = new VariableList<int>();
            for (int y = 0; y < 9; y++)
                ret.Add(XYtoI(x, y));
            return ret;
        }

        VariableList<int> GetRow(int y)
        {
            var ret = new VariableList<int>();
            for (int x = 0; x < 9; x++)
                ret.Add(XYtoI(x, y));
            return ret;
        }



        Func<List<int>, bool> ContainsAllConstraint()
        {
            return (list) =>
            {
                for (int i = 1; i <= 9; i++)
                    if (!list.Contains(i))
                        return false;
                return true;
            };
        }


        int XYtoI(int x, int y)
        {
            return x + y * 9;
        }

        (int x, int y) ItoXY(int i)
        {
            return (i % 9, i / 9);
        }




    }
}

using PC0;
using SadConsole;
using SadConsole.Input;
using System.Net.Http.Headers;

namespace PC0_k_visualizer.Scenes
{

    internal class RootScreen : ScreenObject
    {
        private ScreenSurface _mainSurface;
        private SudokuCellSurface[,] cellSurfaces;
        List<int>[] domains;
        Dictionary<int, Func<int, bool>> unaryConstraints;
        Dictionary<VariableList<int>, Func<List<int>, bool>> constraints;
        Solver<int> solver;

        bool solve = false;
        int RECURSIONDEPTH = 0;
        public RootScreen()
        {
            // Create a surface that's the same size as the screen.
            _mainSurface = new ScreenSurface(GameSettings.GAME_WIDTH, GameSettings.GAME_HEIGHT);

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
            unaryConstraints = new();
            constraints = new();

            // evil
            var board = new int[,]
            {
                {1, 0, 0,  0, 0, 0,  0, 0, 3 },
                {0, 8, 0,  3, 0, 2,  0, 1, 0 },
                {0, 0, 4,  0, 0, 0,  5, 0, 0 },

                {0, 1, 0,  2, 0, 9,  0, 5, 0 },
                {0, 0, 0,  0, 1, 0,  0, 0, 0 },
                {0, 3, 0,  4, 0, 6,  0, 8, 0 },

                {0, 0, 5,  0, 0, 0,  4, 0, 0 },
                {0, 6, 0,  9, 0, 1,  0, 2, 0 },
                {7, 0, 0,  0, 0, 0,  0, 0, 8 },
            };

            // excessive
            /*var board = new int[,]
            {
                {9, 0, 0,  3, 0, 0,  2, 0, 7 },
                {0, 0, 3,  0, 0, 9,  0, 0, 0 },
                {0, 4, 0,  0, 1, 0,  0, 0, 3 },

                {5, 0, 0,  8, 0, 0,  0, 6, 0 },
                {0, 0, 4,  0, 0, 0,  5, 0, 0 },
                {0, 6, 0,  0, 0, 5,  0, 0, 1 },

                {2, 0, 0,  0, 8, 0,  0, 4, 0 },
                {0, 0, 0,  5, 0, 0,  8, 0, 0 },
                {8, 0, 1,  0, 0, 3,  0, 0, 6 },
            };*/

            // the end.
            /*var board = new int[,]
            {
                {8, 0, 0,  0, 0, 0,  0, 0, 0 },
                {0, 0, 3,  6, 0, 0,  0, 0, 0 },
                {0, 7, 0,  0, 9, 0,  2, 0, 0 },

                {0, 5, 0,  0, 0, 7,  0, 0, 0 },
                {0, 0, 0,  0, 4, 5,  7, 0, 0 },
                {0, 0, 0,  1, 0, 0,  0, 3, 0 },

                {0, 0, 1,  0, 0, 0,  0, 6, 8 },
                {0, 0, 8,  5, 0, 0,  0, 1, 0 },
                {0, 9, 0,  0, 0, 0,  4, 0, 0 },
            };*/


            // extreme
            /*var board = new int[,]
            {
                {2, 0, 0,  0, 0, 0,  0, 0, 5 },
                {0, 8, 0,  2, 0, 9,  0, 1, 0 },
                {0, 0, 7,  0, 0, 0,  8, 0, 0 },

                {0, 2, 0,  7, 0, 3,  0, 4, 0 },
                {0, 0, 0,  0, 9, 0,  0, 0, 0 },
                {0, 4, 0,  1, 0, 2,  0, 7, 0 },

                {0, 0, 6,  0, 0, 0,  7, 0, 0 },
                {0, 7, 0,  8, 0, 5,  0, 2, 0 },
                {5, 0, 0,  0, 0, 0,  0, 0, 9 },
            };*/

            // medium
            /*var board = new int[,]
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
            };*/

            // very easy
           /* var board = new int[,]
            {
                {6, 8, 7,  2, 4, 3,  1, 9, 5 },
                {9, 0, 3,  0, 6, 0,  2, 0, 7 },
                {0, 5, 4,  0, 9, 0,  8, 0, 3 },

                {4, 7, 0,  0, 8, 0,  0, 1, 0 },
                {0, 0, 2,  4, 3, 7,  0, 0, 0 },
                {5, 0, 0,  9, 1, 2,  6, 7, 4 },

                {0, 4, 0,  6, 2, 9,  7, 8, 1 },
                {0, 0, 1,  3, 7, 4,  5, 0, 0 },
                {0, 2, 6,  1, 5, 8,  0, 0, 9 },
            };*/

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    // have to flip x and y for indexing into board[] here, because e.g. ROW 3 COLUMN 8 is indexed like this: board[3, 8]
                    // even tho the coordinates of that cell are (8, 3)
                    var val = board[y, x];
                    if (val == 0)
                        continue;

                    var index = XYtoI(x, y);
                    domains[index] = new List<int> { val };
                }
            }

            for (int i = 0; i < 9; i++)
            {
                var row = GetRow(i);
                var column = GetColumn(i);
                var box = GetBox(i);

                int rowID       = i;
                int columnID    = i + 9;
                int boxID       = i + 18;

                for (int j = 0; j < 9; j++)
                {
                    int cell = j == 0 ? 0 : 9 - j;
                    var rowKey = new VariableList<int>(row, rowID, "row " + i + ", cell " + cell);
                    var columnKey = new VariableList<int>(column, columnID, "column " + i + ", cell " + cell);
                    var boxKey = new VariableList<int>(box, boxID, "box " + i + ", cell " + cell);

                    constraints.Add(rowKey, MutuallyExclusiveConstraint);
                    constraints.Add(columnKey, MutuallyExclusiveConstraint);
                    constraints.Add(boxKey, MutuallyExclusiveConstraint);

                    row = row.RotateThrough();
                    column = column.RotateThrough();
                    box = box.RotateThrough();
                }
            }
            solver = new Solver<int>(RECURSIONDEPTH, domains, unaryConstraints, constraints);
            Children.Add(_mainSurface);
            DrawOutline();
        }

        public override bool ProcessKeyboard(Keyboard keyboard)
        {
            if (keyboard.IsKeyPressed(Keys.Space))
                solve = true;
            if (keyboard.IsKeyPressed(Keys.S))
                solve = false;
            return base.ProcessKeyboard(keyboard);
        }




        public override void Update(TimeSpan delta)
        {
            if (solve && !solver.SolveStep())
            {
                if (solver.FailedToSolveReason == Reason.TOO_WEAK)
                {
                    RECURSIONDEPTH += 1;
                    solver = new Solver<int>(RECURSIONDEPTH, domains, unaryConstraints, constraints);
                }
                else
                    solve = false;
            }

            DrawOutline();
            for (int i = 0; i < 81; i++)
            {
                var xy = ItoXY(i);
                cellSurfaces[xy.x, xy.y].DrawDomain(domains[i]);
            }

            _mainSurface.Print(0, 0, RECURSIONDEPTH.ToString());
            base.Update(delta);
        }

        private void DrawOutline()
        {
            var red = new Color(1.0f, 0.0f, 0.0f);
            var green = new Color(0.0f, 1.0f, 0.0f);

            var redHue = red.GetHSLHue();
            var greenHue = green.GetHSLHue();
            var hueStep = (greenHue - redHue) / (9*9);

            int n = 81;
            foreach (var d in domains)
                n -= d.Count == 1 ? 1 : 0;

            var hue = greenHue - n * hueStep;
            var col = Color.FromHSL(hue, 0.4f, 0.5f);


            var boundingBox = new Rectangle(
                _mainSurface.Position.X, _mainSurface.Position.Y,
                _mainSurface.Width, _mainSurface.Height);
            _mainSurface.DrawBox(boundingBox, ShapeParameters.CreateStyledBoxThick(col));

            _mainSurface.DrawLine(new Point(16, 1), new Point(16, 47), 11 * 16 + 10, col);
            _mainSurface.DrawLine(new Point(32, 1), new Point(32, 47), 11 * 16 + 10, col);

            _mainSurface.DrawLine(new Point(1, 16), new Point(47, 16), 12 * 16 + 13, col);
            _mainSurface.DrawLine(new Point(1, 32), new Point(47, 32), 12 * 16 + 13, col);
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

        bool MutuallyExclusiveConstraint(List<int> list)
        {
            for (int i = 0; i < list.Count; i++)
                for (int j = 0; j < list.Count; j++)
                    if (i != j && list[i] == list[j])
                        return false;
            return true;
        }

        Func<List<int>, bool> MutuallyExclusiveConstraint()
        {
            return (list) =>
            {
                for (int i = 0; i < list.Count; i++)
                    for (int j = 0; j < list.Count; j++)
                        if (i != j && list[i] == list[j])
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

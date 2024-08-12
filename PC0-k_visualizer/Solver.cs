using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;

namespace PC0
{
    internal class Solver<T> where T : struct
    {
        List<T>[] domains;
        Dictionary<int, Func<T, bool>> unaryConstraints;
        Dictionary<VariableList<int>, Func<List<T>, bool>> constraints;
        HashSet<VariableList<int>> workList;

        T?[] fixedVariables;

        // the number of constraints that need to be satisfied at the same time
        int maxPaths;
        List<VariableList<int>> paths = new();

        int recursiondepth = 0;

        /// <summary>
        /// Initializes a PC0-k Solver.
        /// </summary>
        /// <param name="maxPaths">The 'k' in "PC0-k". This is the number of constraints that the solver tries to satisfy in each step.</param>
        /// <param name="domains">An array containing a list of possible values for each variable. If domains[0]={1,2,3}, that means the variable 0 can only take values 1, 2 or 3.</param>
        /// <param name="unaryConstraints">Constraints for each variable. If unaryConstraints[0] = (x) => x%2==0, then variable 0 has to be even.</param>
        /// <param name="constraints">Constraints between variables. If constraints[{0,1}] = ({v0, v1}) => v0+v1=5, then the values of variables 0 and 1 need to sum to 5.</param>
        public Solver(
             int maxPaths,
             List<T>[] domains,
             Dictionary<int, Func<T, bool>> unaryConstraints,
             Dictionary<VariableList<int>, Func<List<T>, bool>> constraints)
        {
            this.maxPaths = maxPaths;
            this.domains = domains;
            this.unaryConstraints = unaryConstraints;
            this.constraints = constraints;
            workList = new HashSet<VariableList<int>>();

            fixedVariables = new T?[domains.Count()];

            Setup();
        }
        
        public void Setup()
        {
            MakeInitialDomainsConsistant();
            AddPathsToWorkList();
        }

        /// <summary>
        /// Takes a single step in the solving algorithm
        /// </summary>
        /// <returns>false, if a variable has an empty domain (unsolvable), true otherwise.</returns>
        public bool SolveStep()
        {
            if (workList.IsEmpty())
                return false;
            var workingPath = workList.ElementAt(workList.RandomIndex());
            workList.Remove(workingPath);
            recursiondepth = 0;
            if (MultiplePathTest(workingPath))
            {
                if (domains[workingPath[0]].IsEmpty())
                    return false; // failed to solve

                foreach (var path in GetPaths(workingPath[0]))
                    if (!path.Equals(workingPath))
                        workList.Add(path);
            }
            return true;
        }

        /// <summary>
        /// Runs the solving algorithm as far as it can.
        /// </summary>
        /// <returns>false, if a variable has an empty domain (unsolvable), true otherwise.</returns>
        public bool Solve()
        {
            do
            {
                var workingPath = workList.ElementAt(workList.RandomIndex());
                workList.Remove(workingPath);

                if (PathReduce(workingPath, new List<T>()))
                {
                    if (domains[workingPath[0]].IsEmpty())
                        return false; // failed to solve

                    foreach (var path in GetPaths(workingPath[0]))
                        if (!path.Equals(workingPath))
                            workList.Add(path);
                }
            }
            while (!workList.IsEmpty());
            ;
            return true;
        }

        
        private bool MultiplePathTest(VariableList<int> initialPath)
        {
            // choose overlapping constraints
            paths.Clear();
            paths.Add(initialPath);
            ChooseOtherPaths();

            // value that will get returned
            var changed = false;

            // nicer datastructure for walking the constraint paths
            var pathAsNodes = new List<Node>[paths.Count];
            var fixedByPath = new List<int>[paths.Count];

            // values to use in Consistent(path, values)
            // actual call will be somthing like Consistent(paths[currentPathIndex], values[currentPathIndex]);
            var values = new List<T>[paths.Count];

            // set up nodes
            for(int i = 0; i < paths.Count; i++)
            {
                pathAsNodes[i] = new List<Node>();
                values[i] = new List<T>();
                fixedByPath[i] = new List<int>();
                for(int j = 0; j < paths[i].Count; j++)
                {
                    pathAsNodes[i].Add(new Node(
                        Path:       i,                          // index into paths
                        PathIndex:  j,                          // index into paths[i]
                        Variable:   paths[i][j],                // constraint variable
                        IsPathEnd:  j == paths[i].Count - 1     // is end of current paths[i]
                        ));
                    // list values need to be initialized because they will be overridden using indexing []
                    values[i].Add(default);
                }
            }

            // the actual cell we are checking the domain of
            var rootNode = pathAsNodes[0][0];
            var rootDomain = GetDomain(rootNode.Variable);

            if (rootDomain.Count == 0 || rootDomain.Count == 1)
                return false;

            var currentNodeIndex = 0;
            Node currentNode;
            List<T> currentNodeDomain;

            var currentPathIndex = 0;
            while (true)
            {
                // grab current node and advance one step in the domain
                currentNode = pathAsNodes[currentPathIndex][currentNodeIndex];
                currentNodeDomain = GetDomain(currentNode.Variable);
                currentNode.DomainIndex++;

                // ran out of values to check in current node
                if(currentNode.DomainIndex == currentNodeDomain.Count)
                {
                    if(currentNodeIndex == 0)
                    {
                        if (currentPathIndex == 0)
                            return changed;

                        currentNode.DomainIndex = -1;
                        currentPathIndex--;
                        currentNodeIndex = paths[currentPathIndex].Count - 1;

                        // un-fixing variables
                        foreach (var variable in fixedByPath[currentPathIndex])
                            fixedVariables[variable] = null;
                        fixedByPath[currentPathIndex].Clear();

                        continue;
                    }
                    // if we run out of values to make the current value in the root node work
                    if (currentNodeIndex == 1 && currentPathIndex == 0)
                    {
                        rootDomain.RemoveAt(rootNode.DomainIndex);
                        rootNode.DomainIndex--;
                        changed = true;
                    }
                    currentNode.DomainIndex = -1;
                    currentNodeIndex--;
                    continue;
                }

                values[currentPathIndex][currentNodeIndex] = currentNodeDomain[currentNode.DomainIndex];
                
                if (currentNode.IsPathEnd)
                {
                    if (Consistent(paths[currentPathIndex], values[currentPathIndex]))
                    {
                        if(currentPathIndex < paths.Count - 1) // go one path deeper if we can
                        {

                            // fix current values
                            for(int i = 0; i < paths[currentPathIndex].Count; i++)
                            {
                                var variable = paths[currentPathIndex][i];
                                if (fixedVariables[variable] is not null)
                                    continue;
                                var value = values[currentPathIndex][i];
                                fixedVariables[variable] = value;
                                fixedByPath[currentPathIndex].Add(variable);
                            }

                            currentPathIndex++;
                            currentNodeIndex = 0;
                        }
                        else // otherwise go up to rootnode, resetting everything
                        {
                            // clear all fixed values
                            foreach (var fixedPath in fixedByPath)
                            {
                                for (int i = 0; i < fixedPath.Count; i++)
                                    fixedVariables[fixedPath[i]] = null;
                                fixedPath.Clear();
                            }

                            while(currentNodeIndex > 0 || currentPathIndex > 0)
                            {
                                pathAsNodes[currentPathIndex][currentNodeIndex].DomainIndex = -1;
                                currentNodeIndex--;
                                if(currentNodeIndex < 0 && currentPathIndex > 0)
                                {
                                    currentPathIndex--;
                                    currentNodeIndex = pathAsNodes[currentPathIndex].Count - 1;
                                }
                            }
                        }
                    }
                    continue;
                }
                currentNodeIndex++;
            }
        }


        private bool testing(VariableList<int> path)
        {
            // value that will get returned
            var changed = false;

            // nicer datastructure for walking the constraint path
            var pathAsNodes = new List<Node>(path.Count);

            // values to use in Consistant(path, values)
            var values = new List<T>(path.Count);

            // set up nodes
            for (int i = 0; i < path.Count; i++)
            {
                pathAsNodes.Add(new Node(
                    Path:       0,
                    PathIndex:  i,
                    Variable:   path[i],
                    IsPathEnd:  i == path.Count - 1));

                // list values need to be initialized because they will be overridden using indexing []
                values.Add(default);
            }

            // the actual cell we are checking the domain of
            var rootNode = pathAsNodes[0];
            var rootDomain = GetDomain(rootNode.Variable);

            var currentNodeIndex = 0;
            Node currentNode;
            List<T> currentNodeDomain;
            while (true)
            {
                currentNode = pathAsNodes[currentNodeIndex];
                currentNodeDomain = GetDomain(currentNode.Variable);

                currentNode.DomainIndex++;
                // if we run out of values for the current cell
                if (currentNode.DomainIndex >= currentNodeDomain.Count)
                {
                    // no more values to check in initial domain
                    if (currentNodeIndex == 0)
                        return changed;

                    // if we run out of values to make the current value in the root node work
                    if(currentNodeIndex == 1)
                    {
                        rootDomain.RemoveAt(rootNode.DomainIndex);
                        rootNode.DomainIndex--;
                        changed = true;
                    }

                    currentNode.DomainIndex = -1;
                    currentNodeIndex--;
                    continue;
                } 

                values[currentNodeIndex] = currentNodeDomain[currentNode.DomainIndex];
                if (currentNode.IsPathEnd)
                {
                    // if the current path is consistant, it means that the current value of the root node is valid/satisfiable
                    if(Consistent(path, values))
                    {
                        // go back to root node
                        while (currentNodeIndex > 0)
                        {
                            pathAsNodes[currentNodeIndex].DomainIndex = -1;
                            currentNodeIndex--;
                        }
                    }
                    // if not consistant, keep searching for consistant path
                    continue;
                }
                currentNodeIndex++;
            }
        }

        /// <summary>
        /// Removes all impossible values in the domain of the variable path[0]
        /// </summary>
        /// <param name="path">The starting constraint</param>
        /// <param name="fixedValues">Fixed values along the "path". Should be new List() when called from outside this function</param>
        /// <param name="depth">The position along the path, should be 0 when called from outside this function</param>
        /// <returns>true, if at least one value was removed from the domain of path[0], flase otherwise.</returns>
        private bool PathReduce(VariableList<int> path, List<T> fixedValues, int depth = 0, int k = 0)
        {
            if(depth == 0 && k == 0)
            {
                for (int i = 0; i < fixedVariables.Length; i++)
                    fixedVariables[i] = null;

                paths.Clear();
                paths.Add(path);
                ChooseOtherPaths();
            }

            if (path.Count == fixedValues.Count)
            {
                if (k == (paths.Count - 1))
                    return Consistent(path, fixedValues);

                if (!Consistent(path, fixedValues))
                    return false;

                List<int> added = new();
                // fix current values
                for (int i = 0; i < path.Count; i++)
                {
                    if (fixedVariables[path[i]] is null)
                    {
                        fixedVariables[path[i]] = fixedValues[i];
                        added.Add(path[i]);
                    }
                }

                var valid = PathReduce(paths[k + 1], new List<T>(), 0, k + 1);

                for (int i = 0; i < added.Count; i++)
                    fixedVariables[added[i]] = null;
                return valid;
            }

            var domain = GetDomain(path[depth]);
            var changed = false;
            for (int i = domain.Count - 1; i >= 0; i--)
            {
                var v = domain[i];
                fixedValues.Add(v);
                bool valid = PathReduce(path, fixedValues, depth + 1, k);
                fixedValues.RemoveAt(fixedValues.Count - 1);

                if (valid && (depth != 0 || (k == (paths.Count-1) && depth == 0)))
                    return true;

                if (!valid && depth == 0 && k == 0)
                {
                    changed = true;
                    domain.Remove(v);
                }
            }
            if(depth == 0 && k == 0)
                return changed;   
            return false;
        }

        /// <summary>
        /// Applies unary constraints to each variable
        /// </summary>
        private void MakeInitialDomainsConsistant()
        {
            foreach (var c in unaryConstraints)
            {
                var variable = c.Key;
                var constraint = c.Value;
                var domain = domains[variable];
                for (int i = domain.Count - 1; i >= 0; i--)
                    if (!constraint(domain[i]))
                        domain.RemoveAt(i);
            }
        }

        /// <summary>
        /// Adds all given constraints to the workList
        /// </summary>
        private void AddPathsToWorkList()
        {
            for (int i = 0; i < domains.Length; i++)
            {
                var paths = GetPaths(i);
                foreach (var path in paths)
                    workList.Add(path);
            }
        }

        private void ChooseOtherPaths()
        {
            var possiblePaths = new List<VariableList<int>>();
            foreach(var path in constraints.Keys)
            {
                if(path is null) 
                    continue;

                // if contains original variable
                if (path.Contains(paths[0][0])) 
                    continue;

                // if same id as other one in paths
                var valid = true;
                foreach (var otherPath in possiblePaths)
                    if (path.ID == otherPath.ID) { valid = false; break; }
                if (!valid) 
                    continue;

                var overLapsAtLeastOneOtherPath = false;
                foreach(var otherPath in paths)
                    foreach (var x in otherPath)
                        if (path.Contains(x)) { overLapsAtLeastOneOtherPath = true; break; }
                if (!overLapsAtLeastOneOtherPath) 
                    continue;

                possiblePaths.Add(path);
            }

            int n = possiblePaths.Count;
            for(int i = 0; i < maxPaths && i < n; i++)
            {
                int index = possiblePaths.RandomIndex();
                paths.Add(possiblePaths[index]);
                possiblePaths.RemoveAt(index);
            }
        }

        private List<T> GetDomain(int i)
        {
            if (fixedVariables[i] is null)
                return domains[i];
            else
                return new List<T> { fixedVariables[i]!.Value };

        }

        /// <summary>
        /// Gets a List of all paths containing a variable
        /// </summary>
        /// <param name="i">The variable that must be contained in the paths returned</param>
        /// <returns>The list of paths</returns>
        private List<VariableList<int>> GetPaths(int i)
        {
            List<VariableList<int>> ret = new();
            foreach (var c in constraints)
                if (c.Key.Contains(i))
                    ret.Add(c.Key);
            return ret;
        }
        /// <summary>
        /// Checks if a list of values is consistent with a constraint
        /// </summary>
        /// <param name="variables">The List of variables defining the constraint.</param>
        /// <param name="values">The List of values to check, if they satisfy the constraint.</param>
        /// <returns>True, if the values satisfy the constraint, false otherwise.</returns>
        private bool Consistent(VariableList<int> variables, List<T> values)
        {
            var constraint = constraints[variables];
            return constraint.Invoke(values);
        }
    }
}

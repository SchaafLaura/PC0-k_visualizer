using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;

namespace PC0
{
    enum Reason
    {
        TOO_WEAK,
        INVALID,
        FINISHED
    }
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

        public Reason? FailedToSolveReason { get; private set; } = null;

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
            {
                FailedToSolveReason = Reason.FINISHED;
                foreach(var domain in domains)
                    if(domain.Count > 1)
                        FailedToSolveReason = Reason.TOO_WEAK;
                return false;
            }
                
            var workingPath = workList.ElementAt(workList.RandomIndex());
            workList.Remove(workingPath);
            if (MultiplePathTest(workingPath))
            {
                if (domains[workingPath[0]].IsEmpty())
                {
                    FailedToSolveReason = Reason.INVALID;
                    return false; // failed to solve
                }
                    

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

                if (MultiplePathTest(workingPath))
                {
                    if (domains[workingPath[0]].IsEmpty())
                        return false; // failed to solve

                    foreach (var path in GetPaths(workingPath[0]))
                        if (!path.Equals(workingPath))
                            workList.Add(path);
                }
            }
            while (!workList.IsEmpty());
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

        /// <summary>
        /// Expects a path in the "paths" variable.
        /// Fills up "paths" with others, that overlap at least one other in the List
        /// No other paths will contain paths[0][0] or be the "same" constraint in disguise
        /// </summary>
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

        /// <summary>
        /// Gets the domain of a variable, or a list with a single value in it, if the variable is currently fixed
        /// </summary>
        /// <param name="i">The variable of the domain</param>
        /// <returns>The domain of the variable</returns>
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

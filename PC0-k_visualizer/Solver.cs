namespace PC0
{
    internal class Solver<T>
    {
        List<T>[] domains;
        Dictionary<int, Func<T, bool>> unaryConstraints;
        Dictionary<VariableList<int>, Func<List<T>, bool>> constraints;

        HashSet<VariableList<int>> workList;

        // the number of constraints that need to be satisfied at the same time
        int numPaths;

        /// <summary>
        /// Initializes a PC0-k Solver.
        /// </summary>
        /// <param name="numPaths">The 'k' in "PC0-k". This is the number of constraints that the solver tries to satisfy in each step.</param>
        /// <param name="domains">An array containing a list of possible values for each variable. If domains[0]={1,2,3}, that means the variable 0 can only take values 1, 2 or 3.</param>
        /// <param name="unaryConstraints">Constraints for each variable. If unaryConstraints[0] = (x) => x%2==0, then variable 0 has to be even.</param>
        /// <param name="constraints">Constraints between variables. If constraints[{0,1}] = ({v0, v1}) => v0+v1=5, then the values of variables 0 and 1 need to sum to 5.</param>
        public Solver(
             int numPaths,
             List<T>[] domains,
             Dictionary<int, Func<T, bool>> unaryConstraints,
             Dictionary<VariableList<int>, Func<List<T>, bool>> constraints)
        {
            this.numPaths = numPaths;
            this.domains = domains;
            this.unaryConstraints = unaryConstraints;
            this.constraints = constraints;
            workList = new HashSet<VariableList<int>>();
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
                return true;
            var workingPath = workList.ElementAt(workList.RandomIndex());
            workList.Remove(workingPath);

            if (PathReduce(workingPath, new List<T>(), 0))
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

        /// <summary>
        /// Removes all impossible values in the domain of the variable path[0]
        /// </summary>
        /// <param name="path">The starting constraint</param>
        /// <param name="fixedValues">Fixed values along the "path". Should be new List() when called from outside this function</param>
        /// <param name="depth">The position along the path, should be 0 when called from outside this function</param>
        /// <returns>true, if at least one value was removed from the domain of path[0], flase otherwise.</returns>
        private bool PathReduce(VariableList<int> path, List<T> fixedValues, int depth = 0)
        {
            if (path.Count == fixedValues.Count)
                return Consistent(path, fixedValues);

            var domain = domains[path[depth]];
            var changed = false;
            for (int i = domain.Count - 1; i >= 0; i--)
            {
                var v = domain[i];
                fixedValues.Add(v);
                bool valid = PathReduce(path, fixedValues, depth + 1);
                fixedValues.RemoveAt(fixedValues.Count - 1);

                if (valid && depth != 0)
                    return true;

                if (!valid && depth == 0)
                {
                    changed = true;
                    domain.Remove(v);
                }
            }
            return changed;
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

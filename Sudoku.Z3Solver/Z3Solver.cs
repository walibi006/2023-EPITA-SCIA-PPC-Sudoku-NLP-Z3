﻿using Sudoku.Shared;
using System.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Z3;

namespace Sudoku.Z3Solver
{

    //Pour faire des tests avec les différentes possibilités offertes par l'API, le mieux est sans doute de faire une classe de base abstraite qui mutualise ce dont vous avez besoin, et de faire vos déclinaison dans plusieurs classes qui en héritent. Je vous mets des commentaires directement dans le code pour ça.



	public class Z3Solver: ISudokuSolver
	{
		public SudokuGrid Solve(SudokuGrid s)
		{
            SudokuGrid solution = new SudokuGrid();

            // Le contexte peut sans doute être réutilisé

            using (Context ctx = new Context(new Dictionary<string, string>() { { "model", "true" } }))
            {
                Expr[,] solved = SudokuExample(ctx, s);

                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        solution.Cells[i][j] = int.Parse(solved[i, j].ToString());
                    }
                }
            }
            return solution;
        }

        static Expr[,] SudokuExample(Context ctx, SudokuGrid grid)
        {



            // Début préparation des variables
            // Dans l'objectif de réutiliser au max, vous pouvez peut-être faire de X un champs statique, initialisé dans un constructeur statique de la classe (a priori les variables sont réutilisables)

            // 9x9 matrix of integer variables
            IntExpr[][] X = new IntExpr[9][];
            for (uint i = 0; i < 9; i++)
            {
                X[i] = new IntExpr[9];
                for (uint j = 0; j < 9; j++)
                    X[i][j] = (IntExpr)ctx.MkConst(ctx.MkSymbol("x_" + (i + 1) + "_" + (j + 1)), ctx.IntSort);
            }

            //Fin préparation des variables







			// Début des contraintes "génériques", potentiellement réutilisables pour différents puzzles. Vous pouvez en faire une méthode statique
			// static BoolExpr GetGenericConstraints()
			// et même la stocker derrière une propriété statique du type:
			  //public static BoolExpr GenericContraints
			  //{
				 // get
				 // {
					//  if (_GenericContraints == null)
					//  {
					//	  _GenericContraints = GetGenericConstraints();
					//  }
					//  return _GenericContraints;
				 // }
			  //}


		// each cell contains a value in {1, ..., 9}
		Expr[][] cells_c = new Expr[9][];
            for (uint i = 0; i < 9; i++)
            {
                cells_c[i] = new BoolExpr[9];
                for (uint j = 0; j < 9; j++)
                    cells_c[i][j] = ctx.MkAnd(ctx.MkLe(ctx.MkInt(1), X[i][j]),
                                              ctx.MkLe(X[i][j], ctx.MkInt(9)));
            }


            
            // each row contains a digit at most once
            BoolExpr[] rows_c = new BoolExpr[9];
            for (uint i = 0; i < 9; i++)
                rows_c[i] = ctx.MkDistinct(X[i]);

            // each column contains a digit at most once
            BoolExpr[] cols_c = new BoolExpr[9];
            for (uint j = 0; j < 9; j++)
            {
                IntExpr[] column = new IntExpr[9];
                for (uint i = 0; i < 9; i++)
                    column[i] = X[i][j];

                cols_c[j] = ctx.MkDistinct(column);
            }

            // each 3x3 square contains a digit at most once
            BoolExpr[][] sq_c = new BoolExpr[3][];
            for (uint i0 = 0; i0 < 3; i0++)
            {
                sq_c[i0] = new BoolExpr[3];
                for (uint j0 = 0; j0 < 3; j0++)
                {
                    IntExpr[] square = new IntExpr[9];
                    for (uint i = 0; i < 3; i++)
                        for (uint j = 0; j < 3; j++)
                            square[3 * i + j] = X[3 * i0 + i][3 * j0 + j];
                    sq_c[i0][j0] = ctx.MkDistinct(square);
                }
            }

            BoolExpr sudoku_c = ctx.MkTrue();
            foreach (BoolExpr[] t in cells_c)
                sudoku_c = ctx.MkAnd(ctx.MkAnd(t), sudoku_c);
            sudoku_c = ctx.MkAnd(ctx.MkAnd(rows_c), sudoku_c);
            sudoku_c = ctx.MkAnd(ctx.MkAnd(cols_c), sudoku_c);
            foreach (BoolExpr[] t in sq_c)
                sudoku_c = ctx.MkAnd(ctx.MkAnd(t), sudoku_c);


			// Fin des contraintes "génériques"







			//Début de la contrainte spécifique de puzzle
			// De même vous pouvez l'abstraire dans une méthode
			//  BoolExpr GetPuzzleConstraint(Shared.SudokuGrid instance)
			// Elle n'est pas optimale en l'état, vous pouvez sans doute faire quelque chose du genre:

			//for (int i = 0; i < 9; i++)
			//for (int j = 0; j < 9; j++)
			//	if (grid.Cells[i][j] != 0)
			//	{
			//		instance_c = z3Context.MkAnd(instance_c,
			//			(BoolExpr)
			//			z3Context.MkEq(X[i][j], z3Context.MkInt(grid.Cells[i][j])));
			//	}


			BoolExpr instance_c = ctx.MkTrue();
            for (uint i = 0; i < 9; i++)
                for (uint j = 0; j < 9; j++)
                    instance_c = ctx.MkAnd(instance_c,
                        (BoolExpr)
                        ctx.MkITE(ctx.MkEq(ctx.MkInt(grid.Cells[i][j]), ctx.MkInt(0)),
                                    ctx.MkTrue(),
                                    ctx.MkEq(X[i][j], ctx.MkInt(grid.Cells[i][j]))));


            //Fin de la contrainte de puzzle spécifique







            //Pour le solver, vous pourriez avoir selon les cas une version réutilisable, qui a fait en amont un assert sur les constraintes génériques, et une version à la demande comme les 2 lignes suivantes

            Solver s = ctx.MkSolver();
            s.Assert(sudoku_c);
            s.Assert(instance_c);






			// Exemple de réutilisation en utilisant l'API d'hypothèses (assumptions)

			//BoolExpr instance_c = GetPuzzleConstraint(instance);
			//var z3Solver = GetReusableSolver();
			//if (z3Solver.Check(instance_c) == Status.SATISFIABLE)







			//Autre possibilité: on utilise l'API de Scope pour réutiliser le solver

			//var z3Solver = GetReusableSolver();
			//z3Solver.Push();
			//BoolExpr instance_c = GetPuzzleConstraint(instance);
			//z3Solver.Assert(instance_c);

			//if (z3Solver.Check() == Status.SATISFIABLE)
			//	...
			//z3Solver.Pop();









			// Autre possibilité : on ne réutilise pas le solver, mais on réutilise les contraintes génériques et on utilise l'API de Substitution pour y injecter les valeurs du Sudoku 

			//var substExprs = new List<Expr>();
			//var substVals = new List<Expr>();

			//for (int i = 0; i < 9; i++)
			//for (int j = 0; j < 9; j++)
			//	if (instance.Cells[i][j] != 0)
			//	{
			//		substExprs.Add(X[i][j]);
			//		substVals.Add(z3Context.MkInt(instance.Cells[i][j]));
			//	}

			//BoolExpr instance_c = (BoolExpr)GenericContraints.Substitute(substExprs.ToArray(), substVals.ToArray());

			//var z3Solver = GetSolver();
			//z3Solver.Assert(instance_c);






			

			if (s.Check() == Status.SATISFIABLE)
            {
                Model m = s.Model;
                Expr[,] R = new Expr[9, 9];
                for (uint i = 0; i < 9; i++)
                    for (uint j = 0; j < 9; j++)





	                    //Pour gagner du temps au moment de la récupération de la solution et ne pas passer par des chaines de caractères en aval de cette méthode, vous pourriez avoir du code mettant à jour directement le Sudoku dans le style:

	                    //if (instance.Cells[i][j] == 0)
	                    //{
	                    //	instance.Cells[i][j] = ((IntNum)m.Evaluate(X[i][j])).Int;
	                    //}
                        







						R[i, j] = m.Evaluate(X[i][j]);

                /*Console.WriteLine("Sudoku solution:");
                for (uint i = 0; i < 9; i++)
                {
                    for (uint j = 0; j < 9; j++)
                        Console.Write(" " + R[i, j]);
                    Console.WriteLine();
                }*/
                return R;
            }
            else
            {
                Console.WriteLine("Failed to solve sudoku");
                throw new Exception("Failed to solve sudoku");
            }
        }
    }
}


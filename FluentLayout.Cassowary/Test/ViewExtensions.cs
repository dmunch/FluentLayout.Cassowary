﻿using System;
using Cirrious.FluentLayouts;
using System.Collections.Generic;
using Cassowary;

namespace FluentLayout.Cassowary
{
	class ViewAndLayoutEqualityComparer : IEqualityComparer<ViewAndLayoutAttribute<View>>
	{
		#region IEqualityComparer implementation

		public bool Equals (ViewAndLayoutAttribute<View> x, ViewAndLayoutAttribute<View> y)
		{
			return x.View.Color == y.View.Color && x.Attribute == y.Attribute;
		}

		public int GetHashCode (ViewAndLayoutAttribute<View> obj)
		{
			var hc = string.Format("{0}-{1}", obj.View.Color, obj.Attribute);
			return hc.GetHashCode ();
		}
		#endregion
	}

	public static class ViewExtensions
	{
		static Dictionary<View,ClSimplexSolver> solvers = new Dictionary<View, ClSimplexSolver>();
		static Dictionary<ViewAndLayoutAttribute<View>,ClVariable> variables = 
			new Dictionary<ViewAndLayoutAttribute<View>, ClVariable>(
				new ViewAndLayoutEqualityComparer()
			);

		public static string AddConstraints<T> (this View view, params IFluentLayout<T>[] fluentLayouts)
			where T: View
		{
			ClSimplexSolver solver = null;

			if (!solvers.TryGetValue (view, out solver)) {
				solver = new ClSimplexSolver();
				solvers.Add (view, solver);
			}

			//parent view always stays
			var stayAttributes = new LayoutAttribute[] {
				LayoutAttribute.Left,
				LayoutAttribute.Right,
				LayoutAttribute.Top,
				LayoutAttribute.Bottom
			};

			foreach (var attribute in stayAttributes) {
				var variable = GetVariableFromViewAndAttribute (view, attribute);

				solver.AddStay (variable);
			}

			foreach (var fluentLayout in fluentLayouts) {
				var cn = GetConstraintFromFluentLayout (fluentLayout);
				solver.AddConstraint (cn);
			}
			solver.Solve ();

			//update attributes from solved values
			foreach (var kvp in variables) {
				SetValueFromAttribute (kvp.Key.View, kvp.Key.Attribute, (int)kvp.Value.Value);
			}

			return solver.ToString ();
		}

		public static ClConstraint GetConstraintFromFluentLayout<T>(IFluentLayout<T> fluentLayout)
			where T: View
		{
			ClLinearExpression firstExpression = null;
			firstExpression = GetExpressionFromViewAndAttribute (fluentLayout.View, fluentLayout.Attribute);

			ClLinearExpression secondExpression = null;
			if (fluentLayout.SecondItem != null) {
				secondExpression = GetExpressionFromViewAndAttribute (
					fluentLayout.SecondItem.View,
					fluentLayout.SecondItem.Attribute
				);
				secondExpression = Cl.Plus (
					Cl.Times (secondExpression, fluentLayout.Multiplier),
					new ClLinearExpression (fluentLayout.Constant)
				);
			} else {
				secondExpression = new ClLinearExpression (fluentLayout.Constant);
			}

			ClConstraint cn = null;
			switch (fluentLayout.Relation) {
			case LayoutRelation.Equal:
				cn = new ClLinearEquation (
					firstExpression,
					secondExpression
				);
				break;
			case LayoutRelation.GreaterThanOrEqual:
				cn = new ClLinearInequality (
					firstExpression,
					Cl.GEQ,
					secondExpression
				);
				break;
			case LayoutRelation.LessThanOrEqual:
				cn = new ClLinearInequality (
					firstExpression,
					Cl.LEQ,
					secondExpression
				);
				break;
			}

			cn.Strength = ClStrength.Required;
			return cn;
		}

		public static ClLinearExpression GetExpressionFromViewAndAttribute(View view, LayoutAttribute attribute)
		{
			ClLinearExpression expression = null;
			switch(attribute)
			{
			case LayoutAttribute.Width:
				var leftVar = GetVariableFromViewAndAttribute (view, LayoutAttribute.Left);
				var rightVar = GetVariableFromViewAndAttribute (view, LayoutAttribute.Right);
				expression = Cl.Minus (
					new ClLinearExpression(rightVar), 
					new ClLinearExpression(leftVar)
				);
				break;
			case LayoutAttribute.Height:
				var topVar = GetVariableFromViewAndAttribute (view, LayoutAttribute.Top);
				var bottomVar = GetVariableFromViewAndAttribute (view, LayoutAttribute.Bottom);
				expression = Cl.Minus (
					new ClLinearExpression(bottomVar), 
					new ClLinearExpression(topVar)
				);
				break;
			default:
				var variable = GetVariableFromViewAndAttribute (view, attribute);
				expression = new ClLinearExpression (variable);
				break;
			}

			return expression;
		}

		public static ClVariable GetVariableFromViewAndAttribute(View view, LayoutAttribute attribute)
		{
			ClVariable variable = null;
			var viewAndAttribute = new ViewAndLayoutAttribute<View> (view, attribute);

			if (!variables.TryGetValue (viewAndAttribute, out variable)) {
				var value = GetValueFromAttribute (view, attribute);
				variable = new ClVariable (string.Format("{0}.{1}", view.Color, attribute.ToString()), value);
				variables.Add (viewAndAttribute, variable);
			} else
			{
			}


			return variable; 
		}
		public static int GetValueFromAttribute(View v, LayoutAttribute attribute)
		{
			switch (attribute) {
			case LayoutAttribute.Left:
				return v.Left;
				break;
			case LayoutAttribute.Right:
				return v.Right;
				break;
			case LayoutAttribute.Top:
				return v.Top;
				break;
			case LayoutAttribute.Bottom:
				return v.Bottom;
				break;
			default:
					throw new NotImplementedException(string.Format("Attribute not implemented: {0}", attribute));
			}
		}

		public static void
		SetValueFromAttribute(View v, LayoutAttribute attribute, int value)
		{
			switch (attribute) {
			case LayoutAttribute.Left:
				v.Left = value;
				break;
			case LayoutAttribute.Right:
				v.Right = value;
				break;
			case LayoutAttribute.Top:
				v.Top = value;
				break;
			case LayoutAttribute.Bottom:
				v.Bottom = value;
				break;
			default:
				throw new NotImplementedException(string.Format("Attribute not implemented: {0}", attribute));
			}
		}
	}
}

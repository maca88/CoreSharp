using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CoreSharp.Breeze.Query {
  public class AnyAllPredicate : BasePredicate {

    public object ExprSource { get; private set; }
    public PropBlock NavPropBlock { get; private set; } // calculated as a result of validate;
    public BasePredicate Predicate { get; private set; }


    public AnyAllPredicate(Operator op, object exprSource, BasePredicate predicate) : base(op) {
      ExprSource = exprSource;
      Predicate = predicate;
    }

    public override void Validate(Type entityType) {
      var block = BaseBlock.CreateLHSBlock(ExprSource, entityType);
      if (!(block is PropBlock)) {
        throw new Exception("The first expression of this AnyAllPredicate must be a PropertyExpression");
      }
      this.NavPropBlock = (PropBlock)block;
      var prop = NavPropBlock.Property;
      if (prop.IsDataProperty || prop.ElementType == null) {
        throw new Exception("The first expression of this AnyAllPredicate must be a nonscalar Navigation PropertyExpression");
      }


      this.Predicate.Validate(prop.ElementType);

    }

    public override Expression ToExpression(ParameterExpression paramExpr) {
      var navExpr = NavPropBlock.ToExpression(paramExpr);
      var elementType = NavPropBlock.Property.ElementType;
      MethodInfo mi;
      if (Operator == Operator.Any) {
        mi = TypeFns.GetMethodByExample((IQueryable<string> list) => list.Any(x => x != null), elementType);
      } else {
        mi = TypeFns.GetMethodByExample((IQueryable<string> list) => list.All(x => x != null), elementType);
      }

      var lambdaExpr = Predicate.ToLambda(elementType);
      var castType = typeof(IQueryable<>).MakeGenericType(new Type[] { elementType });
      var castNavExpr = Expression.Convert(navExpr, castType);
      var result = Expression.Call(mi, castNavExpr, lambdaExpr);
      return result;
    }
  }
}

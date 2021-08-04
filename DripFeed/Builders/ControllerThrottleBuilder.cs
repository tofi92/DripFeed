using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Reflection;

namespace DripFeed
{
    public class ControllerThrottleBuilder<TController> where TController : ControllerBase
    {
        private Dictionary<string, ThrottableAction> throttableActions = new Dictionary<string, ThrottableAction>();
        internal IReadOnlyDictionary<string, ThrottableAction> ThrottableActions => throttableActions;

        public ActionThrottleBuilder AddAction(Expression<Func<TController, Func<object>>> actionExpression)
        {
            ThrottableAction throttableAction = new ThrottableAction();

            //TODO Is there a better way to safely cast these?
            var unaryBody = actionExpression.Body as UnaryExpression;
            var operandBody = unaryBody.Operand as MethodCallExpression;
            var action = operandBody.Object as ConstantExpression;
            string actionname = (action.Value as MethodInfo).Name.ToLower();

            throttableAction.ActionName = actionname;
            this.throttableActions.Add(actionname, throttableAction);
            return new ActionThrottleBuilder(throttableAction);
        }
    }
}

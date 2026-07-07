namespace CloudEventSink.Core.Query;

public enum ConditionOperator
{
    Eq = 0,
    Neq = 1,
    Gt = 2,
    Lt = 3,
    Gte = 4,
    Lte = 5,
    Contains = 6,
    In = 7,
    IsNull = 8,
    IsNotNull = 9,
}

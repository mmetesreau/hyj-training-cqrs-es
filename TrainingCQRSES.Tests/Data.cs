using System;
using TrainingCQRSES.Domain.Core;

namespace TrainingCQRSES.Tests;

public static class Data
{
    public static readonly Guid IdentiantPanierA = new("9245fe4a-d402-451c-b9ed-9c1a04247482");
    public static readonly Guid IdentiantPanierB = new("9245fe4a-d402-451c-b9ed-9c1a04247483");
    
    public static readonly Article ArticleA = new("A");
    public static readonly Article ArticleB = new("B");
}
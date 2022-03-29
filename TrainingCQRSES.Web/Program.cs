using Microsoft.AspNetCore.Mvc;
using TrainingCQRSES;
using TrainingCQRSES.Tests;
using TrainingCQRSES.Web;

var builder = WebApplication.CreateBuilder(args);

var eventStore = new InMemoryEventStore();

var panierQueryHandler = new PanierQueryHandler(new PaniersInMemoryRepository());

var eventPublisher = new SimpleEventPublisher(eventStore);
eventPublisher.Subscribe<ArticleAjouteEvt>(panierQueryHandler.Quand);
eventPublisher.Subscribe<ArticleEnleveEvt>(panierQueryHandler.Quand);

builder.Services.AddSingleton<IEventStore>(eventStore);
builder.Services.AddSingleton<IEventPublisher>(eventPublisher);

builder.Services.AddSingleton<PanierQueryHandler>(panierQueryHandler);
builder.Services.AddScoped<PanierCommandHandler>();

var app = builder.Build();

app.MapGet("/api/panier/{panierId}", async (Guid panierId, PanierQueryHandler queryHandler) =>
{
    var panierQuantity = queryHandler.GetQuantity(panierId);

    return Results.Ok(panierQuantity);
});

app.MapPost("/api/panier/{panierId}", async (Guid panierId, [FromBody] ArticleDto dto, PanierCommandHandler commandHandler) =>
{
    await commandHandler
        .Handle(new AjouterArticleCmd(panierId, new Article(dto.IdentifiantArticle)));
    
    return Results.Ok();
});

app.MapDelete("/api/panier/{panierId}", async (Guid panierId, [FromBody] ArticleDto dto, PanierCommandHandler commandHandler) =>
{
    await commandHandler
        .Handle(new EnleverArticleCmd(panierId, new Article(dto.IdentifiantArticle)));
    
    return Results.Ok();
});

app.Run();
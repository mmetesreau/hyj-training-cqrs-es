namespace TrainingCQRSES;

public record PanierQuantite(int NombreArticles);

public class PanierQueryHandler
{
    private readonly IPaniersRepository _panierRepository;

    public PanierQueryHandler(IPaniersRepository panierRepository)
    {
        _panierRepository = panierRepository;
    }

    public PanierQuantite GetQuantity(Guid identifiantPanier)
    {
        return _panierRepository.Get(identifiantPanier);
    }

    public void Quand(ArticleAjouteEvt evt)
    {
        var panierReadModel = _panierRepository.Get(evt.IdentifiantPanier);
        
        _panierRepository.Set(evt.IdentifiantPanier,
            panierReadModel with {NombreArticles = panierReadModel.NombreArticles + 1});
    }

    public void Quand(ArticleEnleveEvt evt)
    {
        var panierReadModel = _panierRepository.Get(evt.IdentifiantPanier);
        
        _panierRepository.Set(evt.IdentifiantPanier,
            panierReadModel with {NombreArticles = panierReadModel.NombreArticles - 1});
    }
}

public interface IPaniersRepository
{
    void Set(Guid id, PanierQuantite value);
    PanierQuantite Get(Guid id);
}
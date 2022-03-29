using TrainingCQRSES.Core.Panier;

namespace TrainingCQRSES.Core.Panier.Queries;

public record Panier(int NombreArticles);

public class PaniersQueryHandler
{
    private readonly IPaniersRepository _panierRepository;

    public PaniersQueryHandler(IPaniersRepository panierRepository)
    {
        _panierRepository = panierRepository;
    }

    public Panier Get(Guid identifiantPanier)
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
    void Set(Guid id, Panier value);
    Panier Get(Guid id);
}
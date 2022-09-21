namespace TrainingCQRSES.Domain.Core;

public class PanierInvalideException : Exception { }

public class Panier
{
    public class DecisionProjection
    {
        private List<Article> _articles = new();

        public IReadOnlyList<Article> Articles
        {
            get => _articles;
        }

        public DecisionProjection Apply(IEvent evt)
        {
            switch (evt)
            {
                case ArticleAjouteEvt articleAjouteEvt:
                    _articles.Add(articleAjouteEvt.Article);
                    break;
                case ArticleEnleveEvt articleEnleveEvt:
                    _articles.Remove(articleEnleveEvt.Article);
                    break;
            }

            return this;
        }
    }

    public static IEvent[] Recoit(AjouterArticleCmd cmd, IEvent[] _)
    {
        return new[] {new ArticleAjouteEvt(cmd.IdentifiantPanier, cmd.Article)};
    }

    public static IEvent[] Recoit(EnleverArticleCmd cmd, IEvent[] histoire)
    {
        var projection = histoire.Aggregate(new DecisionProjection(), (state, evt) => state.Apply(evt));

        if (projection.Articles.All(x => x != cmd.Article)) return Array.Empty<IEvent>();

        return new[] {new ArticleEnleveEvt(cmd.IdentifiantPanier, cmd.Article)};
    }

    public static IEvent[] Recoit(ValiderPanierCmd cmd, IEvent[] histoire)
    {
        var projection = histoire.Aggregate(new DecisionProjection(), (state, evt) => state.Apply(evt));

        if (!projection.Articles.Any()) throw new PanierInvalideException();

        return new[] {new PanierValideEvt(cmd.IdentifiantPanier)};
    }
}

public record Article(string IdentifiantArticle);

public record ArticleAjouteEvt(Guid IdentifiantPanier, Article Article) : IEvent;

public record ArticleEnleveEvt(Guid IdentifiantPanier, Article Article) : IEvent;

public record PanierValideEvt(Guid IdentifiantPanier) : IEvent;

public record AjouterArticleCmd(Guid IdentifiantPanier, Article Article) : ICommand;

public record EnleverArticleCmd(Guid IdentifiantPanier, Article Article) : ICommand;

public record ValiderPanierCmd(Guid IdentifiantPanier) : ICommand;

public interface ICommand { };
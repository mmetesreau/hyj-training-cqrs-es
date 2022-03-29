# Panier

## Step 1 : Domain

- [x] Quand je rajoute un article, alors j’obtiens un évènement ArticleAjouté
- [x] Etant donné un panier avec un article A, quand je valide, alors j’obtiens PanierValidé
- [x] Etant donné un panier avec un article A, quand j’enlève un article A, alors j’obtiens ArticleEnlevé
- [x] Etant donné un panier avec un article A, quand j’enlève un article B, alors je n’émets aucun évènement
- [x] Etant donné un panier vide, quand je valide, alors je retourne une erreur

## Step 2 : Query Handler

- [x] Quand un évènement ArticleAjouté est levé alors le panier est incrémenté
- [x] Quand un évènement ArticleEnlevé est levé alors le panier est décrémenté

## Step 3 : Event Publisher

- [x] Quand un évènement est publié alors il est persisté
- [x] Quand un évènement est publié alors les handlers abonnés sont appelés

## Step 4 : Event Store

- [x] Un event store retourne les évènements d'un aggregat spécifique
- [x] Un event store lève une exception si le numéro de version n'est pas le même

## Step 5 : Command Handler

- [x] Quand je rajoute un article alors le panier est incrémenté
- [x] Quand j'enlève un article alors le panier est décrémenté

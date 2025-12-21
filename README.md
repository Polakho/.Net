# Rapport projet .NET

## Membre de l'équipe

- Clément VANDAMME
- Alex ARMATYS
- Erwan HAIN

## Docker

Le docker est utilisé pour faire fonctionner le Serveur Web ASP et est une part importante du projet. Pour le lancer, il suffit de réaliser la commande

```
docker compose up --build
```

## Serveur ASP

Le serveur ASP est disponible via reverse proxy Nginx à l'adresse https://localhost une fois le docker lancé. Il existe une dizaine d'utilisateurs prédéfinis. Ils sont défini comme suit :

| Email | Mot de passe |
|-------|--------------|
| test{i}@test.com | password |
| admin | adminpassword |

Avec i allant de 1 à 10.

## Jeu 

## Serveur de jeux

## OPTIONS

### Client lourd

Une base simple de client lourd a été réalisée en MAUI. Elle permet de se connecter au serveur ASP et d'afficher une liste de jeux vidéo. Il est possible de filtrer cette liste par nom, prix minimum, prix maximum et catégorie. Il est aussi possible de trier cette liste par nom ou par prix. Un utilisateur connecté peut ajouter un jeu, le télécharger et le lancer.

L'appllication devrait pouvoir fonctionner sur Android en plus de Windows, mais un changement de config est à réaliser dans le fichier Gauniv.Client\Helpers\AppConfig.cs pour changer l'URL du serveur ASP où seront envoyées les requêtes. Cela fonctionnait en utilisant un serveur zrok directement relié au port 443 de la machine locale.
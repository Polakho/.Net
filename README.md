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

Le jeu est un client Godot (C#) de jeu de Go, situé dans `Gauniv.Game` (projet `GoGame`). Il se connecte au serveur de jeux pour : créer une partie, rejoindre une partie à 2 joueurs, ou rejoindre en tant que spectateur.

Pour le lancer, il suffit d'exécuter `Gauniv.Game/GoGame.exe` (une version Windows est déjà fournie dans le dossier) ou d'ouvrir le projet dans Godot 4.5+ et lancer la scène principale.

Par défaut le jeu se connecte au serveur de jeux en TCP sur `127.0.0.1:5000`.

## Serveur de jeux

Le serveur de jeux est un serveur TCP situé dans `Gauniv.GameServer`. Il gère plusieurs parties de Go (plateau, tours, coups, captures, règle du ko, score), avec 2 joueurs maximum par partie et un nombre de spectateurs illimité.

Il écoute sur le port `5000`. Pour le lancer, exécuter le projet `Gauniv.GameServer` (ou lancer `Gauniv.GameServer.exe` si déjà build). Une fois démarré, le client Godot peut créer/rejoindre des parties depuis le lobby.

## OPTIONS

### Client lourd

Une base simple de client lourd a été réalisée en MAUI. Elle permet de se connecter au serveur ASP et d'afficher une liste de jeux vidéo. Il est possible de filtrer cette liste par nom, prix minimum, prix maximum et catégorie. Il est aussi possible de trier cette liste par nom ou par prix. Un utilisateur connecté peut ajouter un jeu, le télécharger et le lancer.

L'appllication devrait pouvoir fonctionner sur Android en plus de Windows, mais un changement de config est à réaliser dans le fichier Gauniv.Client\Helpers\AppConfig.cs pour changer l'URL du serveur ASP où seront envoyées les requêtes. Cela fonctionnait en utilisant un serveur zrok directement relié au port 443 de la machine locale.
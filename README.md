# jump-n-bump-online
Piccolo progettino che ricreare Jump'n'bump ma con il supporto al gioco online.

# Introduzione
Il gioco è basato sull'originale [Jump'n'bump](https://it.wikipedia.org/wiki/Jump_%27n_Bump).
Il progetto non è molto avanzato dal punto di vista tecnico in quanto è stato sviluppato in Sabato pomeriggio di noia.

Ci sono molte cose incomplete:
- I bot sono stupidi.
- Il game loop non è chiuso.
- Il server continua ad accettare connessioni dopo aver avviato la partita.
- Il server usa l'approccio lockstep per applicare l'input dei giocatori mentre fa uso del rollback e raytracing per il detect degli hit.

Principalmente è stato testato con buoni risultati con un ping compreso tra 80-110 ms tra tutti e 4 i giocatori e ottimi risultati su lan (of course).

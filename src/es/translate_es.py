# -*- coding: utf-8 -*-
# Translate OUR custom spoken literals in inject_all_es.csx to Castilian Spanish.
# Only full GML string tokens ("" .. "") are replaced, so GML identifiers,
# in-sentence words and asset/ini keys are never touched.
import io, sys

PATH = "inject_all_es.csx"

# EN inner-text  ->  ES inner-text   (each wrapped in "" .. "" before replacing)
T = {
    # --- directions / positions (internal tokens; translated at every use) ---
    "up": "arriba", "down": "abajo", "left": "izquierda", "right": "derecha",
    "here": "aquí", ", here": ", aquí", "center": "centro", "middle": "en medio",
    "top": "arriba", "bottom": "abajo", " and ": " y ", " of ": " de ",
    " steps": " pasos", " steps.": " pasos.", "Reached": "Has llegado",
    "blank": "en blanco",
    # --- on / off toggle values ---
    "on": "activado", "off": "desactivado",
    # --- scanner kinds / nav labels ---
    "Exit": "Salida", "Exit ": "Salida ", "Going through exit ": "Cruzando la salida ",
    "exits": "salidas",
    "Save": "Guardar", "Save point": "Punto de guardado", "Save point. ": "Punto de guardado. ",
    "Saved.": "Guardado.",
    "No exits found in this room.": "No hay salidas en esta sala.",
    "No interactables here.": "Aquí no hay nada con lo que interactuar.",
    "Nothing interactive nearby.": "No hay nada interactivo cerca.",
    "Nothing within range.": "No hay nada al alcance.",
    "No path found.": "No se ha encontrado ningún camino.",
    "No target selected. Press T to choose one.": "No hay objetivo seleccionado. Pulsa T para elegir uno.",
    "No target. Press tab to choose.": "Sin objetivo. Pulsa tabulador para elegir.",
    "Object": "Objeto",
    "Object scanning and guidance": "Escaneo de objetos y guía",
    "Sign": "Cartel", "Rock": "Roca",
    "Switch": "Interruptor", "Floor switch": "Interruptor de suelo",
    "Switch, press when all are O": "Interruptor, pulsa cuando todos estén en O",
    "Spikes": "Pinchos", "Hole": "Agujero", "Hole ": "Agujero ",
    "Way up": "Salida hacia arriba", "Box": "Caja", "Box. ": "Caja. ",
    "Cell": "Móvil", "Cell phone. ": "Móvil. ",
    "Heart ": "Corazón ", "Soul ": "Alma ",
    "Soul round active. ": "Ronda de almas activa. ",
    "No soul round. ": "No hay ronda de almas. ",
    # --- nav status ---
    "Blocked": "Bloqueado", "Arrived.": "Has llegado.", "Walking.": "Caminando.",
    "Stopped.": "Detenido.", "Path blocked. Stopping.": "Camino bloqueado. Deteniéndose.",
    "You are already there.": "Ya estás ahí.", "Locked in ": "Fijado en ",
    # --- title / settings menus ---
    "Begin Game": "Empezar partida", "Continue": "Continuar", "Reset": "Reiniciar",
    "True Reset": "Reinicio verdadero", "Reset controls": "Restablecer controles",
    "Settings": "Ajustes", "Border": "Borde", "Language, ": "Idioma, ",
    "English": "Inglés", "Japanese": "Japonés",
    "Confirm button": "Botón de confirmar", "Cancel button": "Botón de cancelar",
    "Menu button": "Botón de menú",
    "Press a key to bind it.": "Pulsa una tecla para asignarla.",
    "Load menu. Left or right for Continue or Reset, down for Settings and Accessibility options. On ":
        "Menú de carga. Izquierda o derecha para Continuar o Reiniciar, abajo para Ajustes y Opciones de accesibilidad. ",
    "Title menu. Up or down to choose, Z to confirm. On ":
        "Menú principal. Arriba o abajo para elegir, Z para confirmar. ",
    "Settings. Up and down to move, Z to select, left and right to change a value. On ":
        "Ajustes. Arriba y abajo para moverte, Z para seleccionar, izquierda y derecha para cambiar un valor. ",
    "Undertale. Press the confirm button, Z or Enter, to start.":
        "Undertale. Pulsa el botón de confirmar, Z o Intro, para empezar.",
    # --- accessibility options menu ---
    "Accessibility options": "Opciones de accesibilidad",
    "Accessibility menu closed.": "Menú de accesibilidad cerrado.",
    "Accessibility menu. Up and down to move, left and right to change. Press K or cancel to close. ":
        "Menú de accesibilidad. Arriba y abajo para moverte, izquierda y derecha para cambiar. Pulsa K o cancelar para cerrar. ",
    "Accessibility menu. Up and down to move, left and right to change. Press X or K to close. ":
        "Menú de accesibilidad. Arriba y abajo para moverte, izquierda y derecha para cambiar. Pulsa X o K para cerrar. ",
    "Screen reading": "Lectura de pantalla", "Puzzle skipping": "Saltar puzles",
    "Navigation sounds": "Sonidos de navegación", "Combat cues": "Indicaciones de combate",
    "Assist mode": "Modo asistencia",
    "Assisted, you cannot be defeated": "Asistido, no puedes ser derrotado",
    "Slow, fights run at half speed": "Lento, los combates van a media velocidad",
    "Normal, you can be defeated": "Normal, puedes ser derrotado",
    "Assisted mode. You cannot be defeated.": "Modo asistido. No puedes ser derrotado.",
    "Normal mode. Full speed. You can be defeated.": "Modo normal. Velocidad completa. Puedes ser derrotado.",
    "Slow mode. Fights run at half speed. You can be defeated.": "Modo lento. Los combates van a media velocidad. Puedes ser derrotado.",
    "Option ": "Opción ",
    # --- battle ---
    "Fight": "Luchar", "Fight. ": "Luchar. ", "Act": "Actuar", "Act. ": "Actuar. ",
    "Act on. ": "Actuar activado. ",
    "Item": "Objeto", "Item. ": "Objeto. ", "Items. ": "Objetos. ",
    "Mercy": "Piedad", "Mercy. ": "Piedad. ",
    "Spare": "Perdonar", "Spare, can spare now": "Perdonar, ya puedes perdonar",
    "Flee": "Huir", "Stats": "Estadísticas", "Stats. ": "Estadísticas. ",
    "Your turn. H P ": "Tu turno. PV ", ", H P ": ", PV ", ". HP ": ". PV ",
    "Fight button present. ": "Botón de luchar presente. ",
    "No fight button. ": "No hay botón de luchar. ",
    "Fight button up. Press Z to attack Flowey.": "Botón de luchar activo. Pulsa Z para atacar a Flowey.",
    "Attack. Press Z at the highest beep.": "Ataca. Pulsa Z en el pitido más agudo.",
    "Attacking Flowey.": "Atacando a Flowey.", "Hit. ": "¡Golpe! ",
    "All six souls freed. Flowey is weak now. Press Z to attack him with the fight button.":
        "Las seis almas están libres. Flowey está débil ahora. Pulsa Z para atacarlo con el botón de luchar.",
    "Freeing the soul.": "Liberando el alma.",
    "Auto freeing the soul.": "Liberando el alma automáticamente.",
    "Soul round. Press Z to free this soul. Follow the beep to it if you want, but Z works from anywhere.":
        "Ronda de almas. Pulsa Z para liberar esta alma. Sigue el pitido si quieres, pero Z funciona desde cualquier sitio.",
    # --- shop ---
    "Buy": "Comprar", "Buy. ": "Comprar. ", "Sell": "Vender",
    "Talk": "Hablar", "Talk. ": "Hablar. ", "Shop. ": "Tienda. ",
    "Yes, buy for ": "Sí, comprar por ", "You have ": "Tienes ",
    # --- choices ---
    "Choice. ": "Elección. ", "Choose action. ": "Elige una acción. ", "Choose. ": "Elige. ",
    "Choose. Move your heart onto a box and press Z. You will hear each box as you reach it.":
        "Elige. Mueve tu corazón hasta una casilla y pulsa Z. Oirás cada casilla al llegar a ella.",
    "You can kill or spare him. Kill is on the left. Spare is on the right. Move your heart onto a box, then press Z.":
        "Puedes matarlo o perdonarlo. Matar está a la izquierda. Perdonar está a la derecha. Mueve tu corazón hasta una casilla y pulsa Z.",
    "Kill": "Matar", "Question. ": "Pregunta. ", "Yes": "Sí", "Confirm. ": "Confirmar. ",
    # --- naming ---
    "Backspace": "Retroceso", "Done": "Hecho", "Quit": "Salir",
    "Character": "Carácter", "Character set": "Cambiar mayúsculas",
    "Name ": "Nombre ", "Empty": "Vacío",
    "Name entry. Arrow keys move, Z selects a letter, X deletes. Spell a name, then choose Done.":
        "Introducir nombre. Las flechas mueven, Z selecciona una letra, X borra. Escribe un nombre y elige Hecho.",
    # --- interact / item actions ---
    "Info": "Información", "Read": "Leer", "Take": "Coger", "Use": "Usar",
    "Drop": "Tirar", "Return": "Volver", "Go back": "Volver atrás", "Steal": "Robar",
    # --- puzzle skip / assists ---
    "Puzzle skipped. Solving.": "Puzle saltado. Resolviendo.",
    "Skip cancelled.": "Salto cancelado.", "Skip mode. ": "Modo salto. ",
    "Skip puzzle? Press O to confirm.": "¿Saltar el puzle? Pulsa O para confirmar.",
    "Shooting puzzle. This one is visual. To skip it, press P then O.":
        "Puzle de disparos. Este es visual. Para saltarlo, pulsa P y luego O.",
    "Shield. Block each spear with the arrow toward it. Beep: left or right ear is left or right, high pitch is up, low pitch is down.":
        "Escudo. Bloquea cada lanza con la flecha hacia ella. Pitido: oído izquierdo o derecho es izquierda o derecha, tono agudo es arriba, tono grave es abajo.",
    "O tile": "casilla O", "O, done, avoid": "O, hecho, evita", "X, step on it": "X, písala",
    "Diagnostic. ": "Diagnóstico. ",
    "Music ": "Música ", "Music off": "Música desactivada", "Music volume": "Volumen de música",
}

with io.open(PATH, "r", encoding="utf-8") as f:
    text = f.read()

# Longest keys first so a short key can never pre-empt a longer literal.
applied, missing = 0, []
for k in sorted(T, key=len, reverse=True):
    tok = '""' + k + '""'
    n = text.count(tok)
    if n == 0:
        missing.append(k)
        continue
    text = text.replace(tok, '""' + T[k] + '""')
    applied += n

with io.open(PATH, "w", encoding="utf-8") as f:
    f.write(text)

print("tokens replaced:", applied)
print("keys applied:", len(T) - len(missing), "/", len(T))
print("KEYS NOT FOUND (", len(missing), "):")
for m in missing:
    print("   ", repr(m))

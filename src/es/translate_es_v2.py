# -*- coding: utf-8 -*-
# Spanish translation engine v2 (2026-07-08). Builds inject_all_es.csx from the CURRENT English
# inject_all.csx by replacing every spoken string with its Spanish version.
#
# Two string forms exist and both are handled:
#   (A) GML-verbatim doubled quotes  ""text""  -> menus/UI + character & monster descriptions
#   (B) plain C# string literals      "text"   -> args to the beat helpers (AmbBeat/ObjBeat/...)
# Doubled form is applied for ALL keys; single form ONLY for keys that appear as beat args
# (loaded from _corpus_beats.txt) so short UI tokens can never corrupt C# code. Longest-first.
import io, re, ast

SRC = r"C:\Users\hp\UndertaleAccess\work\inject_all.csx"
OUT = "inject_all_es.csx"

# --- reuse the existing native-ES UI translations (ArceUseless) from translate_es.py ---
tsrc = io.open("translate_es.py", encoding="utf-8").read()
mm = re.search(r'\nT\s*=\s*\{.*?\n\}', tsrc, re.S)
T_ui = ast.literal_eval(mm.group(0).split('=', 1)[1].strip())

# --- NEW translations (filled in batches). EN -> ES ---
T_ad = {
    # ===== character descriptions (batch 1) =====
    "A small golden flower pokes up through the ground. It has a round white face, two big dark eyes and a wide, cheerful smile.":
        "Una pequeña flor dorada asoma por el suelo. Tiene una cara redonda y blanca, dos grandes ojos oscuros y una amplia sonrisa alegre.",
    "A tall, gentle monster who looks like a goat standing on two legs. She has soft white fur, long floppy ears, small horns and warm eyes, and wears a long purple robe marked with a white winged crest.":
        "Un monstruo alto y amable que parece una cabra erguida sobre dos patas. Tiene un pelaje blanco y suave, largas orejas caídas, cuernos pequeños y ojos cálidos, y viste una larga túnica morada con un escudo alado blanco.",
    "A short, pudgy skeleton with a wide, permanent grin. He wears a blue hooded jacket over a white shirt, black shorts, and pink slippers, and looks completely relaxed.":
        "Un esqueleto bajito y rechoncho con una amplia sonrisa permanente. Lleva una chaqueta azul con capucha sobre una camiseta blanca, pantalones cortos negros y zapatillas rosas, y parece completamente relajado.",
    "A very tall, lanky skeleton striking a dramatic pose. He wears a homemade costume: a white chest piece with a long red scarf, red gloves and red boots.":
        "Un esqueleto muy alto y larguirucho que adopta una pose dramática. Lleva un disfraz casero: un peto blanco con una larga bufanda roja, guantes rojos y botas rojas.",
    "A tall, powerful fish-like monster with deep blue scales, a long red ponytail and sharp yellow teeth. One eye is covered by a black eyepatch, and she wears gleaming metal armour.":
        "Un monstruo alto y poderoso con aspecto de pez, de escamas azul oscuro, una larga coleta roja y afilados dientes amarillos. Un ojo lo lleva cubierto con un parche negro, y viste una reluciente armadura de metal.",
    "A short, round, yellow lizard-like monster with glasses and a slightly nervous expression. She wears a white lab coat.":
        "Un monstruo bajito y regordete de color amarillo, con aspecto de lagarto, gafas y una expresión algo nerviosa. Viste una bata blanca de laboratorio.",
    "An enormous, powerful monster like a goat standing upright, built like a broad-shouldered king. He has white fur, long curved horns, a golden mane and beard, and wears purple armour.":
        "Un monstruo enorme y poderoso, como una cabra erguida, con la corpulencia de un rey de anchos hombros. Tiene pelaje blanco, largos cuernos curvos, melena y barba doradas, y viste una armadura morada.",
    "A young goat-like monster child with soft white fur, long floppy ears and small horns. He wears a green robe with a single yellow stripe across the middle.":
        "Un joven monstruo con aspecto de cabra, un niño de pelaje blanco y suave, largas orejas caídas y cuernos pequeños. Viste una túnica verde con una única franja amarilla en el centro.",
    "A robot shaped like a rectangular metal box balanced on a single wheel. Its front is covered in dials, buttons and a small screen, and two slim white-gloved arms reach out from its sides.":
        "Un robot con forma de caja metálica rectangular que se sostiene sobre una única rueda. Su parte frontal está cubierta de diales, botones y una pequeña pantalla, y de sus costados salen dos brazos delgados con guantes blancos.",
    "A strange little creature drawn in a crude, scribbly style: a cat-like face with big ears and wide eyes on a small brown furry body.":
        "Una pequeña y extraña criatura dibujada con un estilo tosco y garabateado: una cara felina de grandes orejas y ojos muy abiertos sobre un pequeño cuerpo peludo de color marrón.",
    "A tall, shadowy figure that seems to melt at the edges. Its pale, cracked face has one crack running up from an eye and another running down, and it speaks in strange symbols.":
        "Una figura alta y sombría que parece derretirse por los bordes. Su rostro pálido y agrietado tiene una grieta que sube desde un ojo y otra que baja, y habla mediante símbolos extraños.",

    # ===== back-half finale cutscene beats (batch 1) =====
    "In a burst of light and confetti the boxy robot's true form unfolds: a tall, glamorous machine with long slender legs, one arm flung out in a pose, dark hair swept over one eye, and a glowing heart set in his chest. Mettaton EX has arrived, and he is fabulous.":
        "Entre un estallido de luz y confeti, la verdadera forma del robot con forma de caja se despliega: una máquina alta y glamurosa de piernas largas y esbeltas, un brazo extendido en una pose, el pelo oscuro cayéndole sobre un ojo y un corazón brillante en el pecho. Mettaton EX ha llegado, y es fabuloso.",
    "The entire cooking counter blasts up off the stage and rockets into the sky, Mettaton riding it away. A phone rings, a jetpack straps itself to your back, and you shoot upward after him, hurtling through the open air.":
        "Toda la encimera de cocina sale disparada del escenario y se eleva hacia el cielo, con Mettaton subido en ella. Suena un teléfono, una mochila propulsora se ata a tu espalda y sales disparado tras él, surcando el aire.",
    "At the far end of the hall a huge, broad figure in royal robes stands with his back to you, great curved horns bowed as he gently waters the bed of golden flowers, humming softly to himself, not yet aware that anyone has come.":
        "Al fondo de la sala, una figura enorme y corpulenta con ropas reales está de espaldas a ti, con sus grandes cuernos curvos inclinados mientras riega con delicadeza el lecho de flores doradas, tarareando para sí mismo, aún sin advertir que alguien ha llegado.",
    "The King lifts his great trident and brings it down on the MERCY button, smashing it to splinters. There is no talking your way out now, and no sparing him - only the fight.":
        "El Rey alza su gran tridente y lo descarga sobre el botón de PIEDAD, haciéndolo añicos. Ya no hay forma de dialogar, ni de perdonarlo: solo queda luchar.",
    "Six upturned hearts drift up into the dark and hang in the air around you, each a different colour: cyan, orange, blue, purple, green and gold. The six human souls the King has gathered, all that is needed to break the barrier at last.":
        "Seis corazones invertidos ascienden flotando en la oscuridad y quedan suspendidos en el aire a tu alrededor, cada uno de un color distinto: cian, naranja, azul, morado, verde y dorado. Las seis almas humanas que el Rey ha reunido, todo lo necesario para al fin romper la barrera.",
    "The whole screen lurches and cracks. Three thunderous blows split it like glass, and your very SAVE file breaks apart and is wiped away into nothing. Out of the darkness rises a small golden flower, wearing a wide, grinning face.":
        "Toda la pantalla se sacude y se resquebraja. Tres golpes atronadores la parten como el cristal, y tu propio archivo de GUARDADO se hace pedazos y se borra hasta no quedar nada. De la oscuridad surge una pequeña flor dorada, con una amplia y burlona sonrisa.",
    "The flower has fused himself with your stolen SAVE and the six souls into something monstrous: a towering hulk of steel, cables and television screens, machinery grinding and flashing all around a huge, distorted face that leers out from the very centre of it. He looms over you, vast and grinning.":
        "La flor se ha fusionado con tu GUARDADO robado y las seis almas para convertirse en algo monstruoso: una mole colosal de acero, cables y pantallas de televisión, con maquinaria que zumba y destella alrededor de un rostro enorme y deforme que se asoma desde su mismo centro. Se cierne sobre ti, inmenso y sonriente.",
    "One by one the six human souls flare back to life inside the machine and turn against the flower, circling around you, shielding you and healing your wounds as they rise up against their captor.":
        "Una a una, las seis almas humanas vuelven a cobrar vida dentro de la máquina y se rebelan contra la flor, rodeándote, protegiéndote y curando tus heridas mientras se alzan contra su captor.",

    # ===== monster describe key (D) descriptions (batch 2) =====
    "A plump little frog-like monster, pale and soft, that sits blinking up at you with big round eyes and a wide, gentle mouth.":
        "Un monstruo pequeño y rechoncho con aspecto de rana, pálido y blando, que se queda pestañeando ante ti con grandes ojos redondos y una boca ancha y apacible.",
    "A tiny, timid winged monster like a small moth. It has a little downturned face and trembles nervously, as if it is sorry to be fighting at all.":
        "Un monstruo alado, diminuto y tímido, parecido a una pequeña polilla. Tiene una carita cabizbaja y tiembla con nerviosismo, como si lamentara siquiera estar luchando.",
    "A wobbly mound of pale green jelly-mold. It jiggles gently in place and does not seem to have any idea how to hurt anyone.":
        "Un montículo tembloroso de moho gelatinoso de color verde pálido. Se menea con suavidad en el sitio y no parece tener la menor idea de cómo hacer daño a nadie.",
    "A small dark beetle-like bug monster with round eyes and little waving arms. Away from a crowd it is shy and harmless.":
        "Un pequeño monstruo insecto de color oscuro, parecido a un escarabajo, con ojos redondos y bracitos que agita. Lejos de la multitud es tímido e inofensivo.",
    "A big, friendly vegetable monster rising out of the ground, shaped like a giant carrot with a wide grinning face full of blocky teeth.":
        "Un monstruo vegetal, grande y amistoso, que emerge del suelo con forma de zanahoria gigante y una amplia cara sonriente llena de dientes cuadrados.",
    "A round little monster covered in soft fur, with one big eye in the middle of its face. It looks grumpy, but really it just does not want to be picked on.":
        "Un monstruo pequeño y redondo cubierto de pelaje suave, con un gran ojo en el centro de la cara. Parece malhumorado, pero en realidad solo no quiere que se metan con él.",
    "A small, shy white ghost with a droopy face and half-closed sleepy eyes. It drifts quietly and always seems on the edge of tears.":
        "Un pequeño y tímido fantasma blanco de cara caída y ojos somnolientos entornados. Flota en silencio y siempre parece a punto de llorar.",
    "A cloth training dummy on a wooden stand, with a plain stitched-on face. It stands there silently, waiting.":
        "Un muñeco de entrenamiento de tela sobre un soporte de madera, con una cara sencilla cosida. Permanece ahí en silencio, esperando.",
    "The tall, gentle goat-like monster in a long purple robe, standing between you and the door with a pained, protective look.":
        "El monstruo alto y amable con aspecto de cabra, con su larga túnica morada, interponiéndose entre tú y la puerta con una mirada dolida y protectora.",
    "A small blue bird-monster built for the cold, with a crest of icy feathers. A teenager doing its best to land a good joke.":
        "Un pequeño monstruo pájaro de color azul, hecho para el frío, con una cresta de plumas heladas. Un adolescente que se esfuerza al máximo por soltar un buen chiste.",
    "A little furry monster nearly hidden beneath an enormous pointed cap of ice that it is extremely proud of. Only its eyes peek out from underneath.":
        "Un pequeño monstruo peludo casi oculto bajo un enorme gorro puntiagudo de hielo del que está sumamente orgulloso. Solo sus ojos asoman por debajo.",
    "A shaggy, weary deer-like monster whose antlers have been draped with junk by prank-playing kids. It just wants them taken off.":
        "Un monstruo peludo y fatigado con aspecto de ciervo, cuya cornamenta han cubierto de trastos unos niños bromistas. Solo quiere que se los quiten.",
    "A dog-monster sitting in a wooden sentry post, gripping two glowing daggers, eyes darting about. It can only see things that move.":
        "Un monstruo perro sentado en una garita de madera, empuñando dos dagas brillantes y con los ojos yendo de un lado a otro. Solo puede ver lo que se mueve.",
    "A small white dog-monster in a suit of armour, holding a sword, with a cheerfully lolling tongue and a neck that stretches longer the more excited it gets.":
        "Un pequeño monstruo perro blanco con una armadura, espada en mano, con la lengua colgando alegremente y un cuello que se estira más cuanto más se emociona.",
    "A little white dog almost lost inside a huge suit of armour, tail wagging eagerly. It would much rather play than fight.":
        "Un pequeño perro blanco casi perdido dentro de una enorme armadura, moviendo la cola con entusiasmo. Preferiría con creces jugar antes que luchar.",
    "One of a married pair of dog-monsters in black hooded robes, swinging a large axe and sniffing the air for your scent.":
        "Uno de un matrimonio de monstruos perro con túnicas negras con capucha, blandiendo un hacha grande y olfateando el aire en busca de tu olor.",
    "One of a married pair of dog-monsters in black hooded robes, padding beside her husband with an axe in paw.":
        "Una de un matrimonio de monstruos perro con túnicas negras con capucha, caminando junto a su marido con un hacha en la pata.",
    "The very tall, lanky skeleton in his homemade white costume and long red scarf, striking a heroic pose as he faces you.":
        "El esqueleto muy alto y larguirucho con su disfraz blanco casero y su larga bufanda roja, adoptando una pose heroica mientras te hace frente.",
    "A cheeky blue bird-monster, cousin of the Snowdrake, with an icy feathered crest and an even cooler attitude.":
        "Un descarado monstruo pájaro azul, primo del Snowdrake, con una cresta de plumas heladas y una actitud aún más fría.",
    "A cloth dummy floating in the air, shaking with rage as an angry ghost throws its voice through it. Its stitched face is twisted into a furious scowl.":
        "Un muñeco de tela que flota en el aire, temblando de rabia mientras un fantasma furioso proyecta su voz a través de él. Su cara cosida está retorcida en una mueca furiosa.",
    "A muscular seahorse-like monster with a confident grin, endlessly flexing his enormous arms. He is far more interested in showing off than fighting.":
        "Un musculoso monstruo con aspecto de caballito de mar y una sonrisa segura de sí misma, que no para de marcar sus enormes brazos. Le interesa mucho más presumir que luchar.",
    "A small blue monster shaped a bit like a crab, with a scrubbing brush for a crest. It is obsessed with cleanliness and just wants everything tidy.":
        "Un pequeño monstruo azul con forma parecida a un cangrejo y un cepillo de fregar por cresta. Está obsesionado con la limpieza y solo quiere que todo esté ordenado.",
    "A tall, wobbling column of pale green mold, like a taller cousin of the Moldsmal. It sways gently and loves a good hug.":
        "Una columna alta y temblorosa de moho verde pálido, como un primo más alto del Moldsmal. Se balancea con suavidad y le encantan los buenos abrazos.",
    "A shy mermaid-like monster who hides her face, humming a quiet, wavering tune. She is far too bashful to look at you directly.":
        "Un tímido monstruo con aspecto de sirena que oculta su rostro, tarareando una melodía suave y temblorosa. Es demasiado vergonzosa como para mirarte directamente.",
    "A fuzzy, cloud-like monster in a bomber jacket, drifting with an awkward, trying-too-hard cool. A rare wanderer not quite sure it belongs.":
        "Un monstruo esponjoso con aspecto de nube y una cazadora bómber, que flota con una chulería torpe y demasiado forzada. Un raro vagabundo que no acaba de tener claro que encaje.",
    "A small, lumpy grey monster with a permanently smug, oblivious expression. Frankly, nobody enjoys having Jerry around.":
        "Un pequeño monstruo gris y grumoso con una expresión permanentemente engreída y ajena a todo. Francamente, a nadie le gusta tener cerca a Jerry.",
    "A tall, powerful fish-warrior in gleaming armour, red hair streaming and one eye blazing behind an eyepatch as she summons glowing spears. The head of the Royal Guard, and she will not back down.":
        "Una alta y poderosa guerrera pez con reluciente armadura, la melena roja al viento y un ojo ardiendo tras un parche mientras invoca lanzas brillantes. La jefa de la Guardia Real, y no piensa dar su brazo a torcer.",
    "A small, round volcano-monster glowing warm at its crater, with stubby arms and an eager, beaming face. It only wants to help, even when its help burns a little.":
        "Un pequeño y redondo monstruo volcán que brilla cálido en su cráter, con bracitos rechonchos y una cara radiante y entusiasta. Solo quiere ayudar, aunque su ayuda queme un poco.",
    "A fighter-plane monster with a blushing, bashful face on its nose. It insists it is absolutely not flying this close to you on purpose.":
        "Un monstruo avión de combate con una cara ruborizada y vergonzosa en el morro. Insiste en que desde luego no vuela tan cerca de ti a propósito.",
    "A round, flaming monster like a living ember with a wide grin, radiating heat and wanting everything hotter.":
        "Un monstruo redondo y llameante, como una brasa viviente con una amplia sonrisa, que irradia calor y lo quiere todo más caliente.",
    "A tall, cloaked wizard-monster in a wide, pointed hat, with two glowing magic orbs circling it. Only its glinting eyes show beneath the brim.":
        "Un alto monstruo mago con capa y un sombrero ancho y puntiagudo, con dos orbes mágicos brillantes girando a su alrededor. Solo se le ven los ojos centelleantes bajo el ala del sombrero.",
    "An enormous, heavily armoured monster like a mountain of a knight, with a crescent-moon helm and a massive mace. Slow, sleepy, and immensely strong.":
        "Un monstruo enorme y muy acorazado, como un caballero descomunal, con un yelmo en forma de luna creciente y una maza descomunal. Lento, somnoliento e inmensamente fuerte.",
    "A tougher, older Froggit from the deeper Underground. The same soft, pale frog body and big eyes, but with a wiser, more determined look.":
        "Un Froggit más duro y veterano de las profundidades del Subsuelo. El mismo cuerpo de rana, blando y pálido, y los mismos grandes ojos, pero con una mirada más sabia y decidida.",
    "A Whimsun grown into a tiny armoured knight, with little wings, a helmet and a spear, bravely trying to look fierce.":
        "Un Whimsun convertido en un diminuto caballero acorazado, con alitas, un casco y una lanza, tratando con valentía de parecer feroz.",
    "A round yellow monster with one big eye and a wide, toothy mouth, glaring sharply. It is very insistent that you pay attention.":
        "Un monstruo amarillo y redondo con un gran ojo y una boca ancha y dentuda, con la mirada fija y severa. Insiste mucho en que prestes atención.",
    "A Migosp that has found its confidence: a little beetle-monster waving its arms cheerfully to a beat only it can hear.":
        "Un Migosp que ha encontrado su confianza: un pequeño monstruo escarabajo que agita los brazos con alegría al ritmo de una música que solo él oye.",
    "A small, flustered dragon-like monster in formal wear, tripping over its own apologies. It genuinely did not mean to be here and is terribly sorry.":
        "Un pequeño y azorado monstruo con aspecto de dragón, de traje formal, que se enreda en sus propias disculpas. De verdad que no pretendía estar aquí y lo siente muchísimo.",
    "An elegant purple spider-monster with five eyes and five arms, in a frilly outfit, a teacup balanced daintily in one hand as her pet spiders skitter around her.":
        "Un elegante monstruo araña de color morado, con cinco ojos y cinco brazos, con un atuendo de volantes y una taza de té sostenida con delicadeza en una mano mientras sus arañas mascota corretean a su alrededor.",
    "The rectangular metal box robot on a single wheel, dials and a screen on its front and slim arms out to either side, hosting this like a dazzling TV show.":
        "El robot con forma de caja metálica rectangular sobre una única rueda, con diales y una pantalla en la parte frontal y brazos delgados a cada lado, presentando todo esto como un deslumbrante programa de televisión.",
    "A fabulous robot in a sleek humanoid form, all black and pink, balanced on one wheeled leg in a dramatic pose under the stage lights.":
        "Un robot fabuloso con una elegante forma humanoide, todo en negro y rosa, sostenido sobre una única pierna con rueda en una pose dramática bajo las luces del escenario.",
    "A towering battle-form robot bristling with cannons and armour, wings spread, built to look utterly unstoppable.":
        "Un robot descomunal en forma de combate, erizado de cañones y blindaje, con las alas desplegadas, hecho para parecer absolutamente imparable.",
    "An enormous goat-king in purple armour over a royal cape, huge and broad-shouldered, with long curved horns and a golden beard. He lifts a great trident, and his eyes are full of sorrow.":
        "Un enorme rey cabra con armadura morada sobre una capa real, inmenso y de anchos hombros, con largos cuernos curvos y una barba dorada. Alza un gran tridente, y sus ojos están llenos de pesar.",
    "The short skeleton in the blue hooded jacket, hands in his pockets, grinning as ever, one eye flickering with a strange blue light. He does not seem to be taking this seriously, which is the most dangerous thing about him.":
        "El esqueleto bajito con la chaqueta azul con capucha, las manos en los bolsillos, sonriendo como siempre, con un ojo parpadeando con una extraña luz azul. No parece tomarse esto en serio, y eso es lo más peligroso de él.",
    "A large, unsettling amalgam of many dog-monsters melted together, a dripping white mass with a single dog-like face and too many limbs. It bounds toward you wanting to play.":
        "Una amalgama grande e inquietante de muchos monstruos perro fundidos entre sí, una masa blanca y goteante con una única cara canina y demasiadas extremidades. Se abalanza hacia ti con ganas de jugar.",
    "A tall, eerie amalgam of bird-monsters, dark-winged with a long neck and a hollow, staring face. It drifts unnaturally, several beings at once.":
        "Una amalgama alta y espeluznante de monstruos pájaro, de alas oscuras, cuello largo y un rostro hueco de mirada fija. Se desplaza de forma antinatural, varios seres a la vez.",
    "A pale amalgam of monster-parts fused into a long, finned, snake-like body with too many mouths, weaving with an odd grace.":
        "Una amalgama pálida de partes de monstruo fundidas en un cuerpo largo y serpenteante con aletas y demasiadas bocas, que se contonea con una extraña elegancia.",
    "A drifting, half-formed amalgam like a melting white face on a stalk, murmuring softly and reaching toward you as if it knows you.":
        "Una amalgama flotante y a medio formar, como un rostro blanco que se derrite sobre un tallo, que murmura suavemente y se extiende hacia ti como si te conociera.",
    "A large, gentle bird-monster, mother of the little Snowdrake, her feathers pale and soft. She carries a quiet, tired sadness.":
        "Un monstruo pájaro grande y apacible, madre del pequeño Snowdrake, de plumas pálidas y suaves. Arrastra una tristeza callada y cansada.",
    "A hard-mode cousin of the Moldsmal: a wobbling mound of mold with a bit more attitude.":
        "Un primo del Moldsmal en modo difícil: un montículo tembloroso de moho con algo más de carácter.",
    "A hard-mode cousin of the Vegetoid: a large root-vegetable monster with a snappier grin.":
        "Un primo del Vegetoid en modo difícil: un gran monstruo tubérculo con una sonrisa más mordaz.",
    "No description written yet for ":
        "Aún no hay descripción escrita para ",

    # ===== ROOM AMBIANCE: Ruins + Toriel's home + basement (batch 3) =====
    "You land unhurt in a bed of golden flowers, deep underground. Pale light filters down from a hole high above.":
        "Aterrizas ileso en un lecho de flores doradas, en las profundidades del subsuelo. Una luz pálida se filtra desde un agujero en lo alto.",
    "A small stone chamber. A pressure plate is set into the floor, and a lever juts from the wall. Pressing both opens the door ahead.":
        "Una pequeña cámara de piedra. Hay una placa de presión encastrada en el suelo y una palanca que sobresale de la pared. Accionar ambas abre la puerta de delante.",
    "A room crossed by a bed of spikes. Rows of switches line the walls, and Toriel has marked the ones you need with arrows, so you can lower the spikes and cross safely.":
        "Una sala cruzada por un lecho de pinchos. Hileras de interruptores recorren las paredes, y Toriel ha marcado con flechas los que necesitas, para que puedas bajar los pinchos y cruzar sin peligro.",
    "A cloth training dummy stands on a wooden stand in the middle of the room, its stitched face blank and patient.":
        "Un muñeco de entrenamiento de tela se alza sobre un soporte de madera en el centro de la sala, con su cara cosida inexpresiva y paciente.",
    "A long hall. Patches of spikes stud the floor, with a safe path winding between them.":
        "Un largo pasillo. Zonas de pinchos salpican el suelo, con un camino seguro que serpentea entre ellas.",
    "A long, straight corridor stretches away into the gloom, its far end lost in shadow. The air is still, and your own quiet footsteps are the only sound.":
        "Un pasillo largo y recto se extiende hacia la penumbra, con el extremo perdido en la sombra. El aire está quieto, y tus propios pasos silenciosos son el único sonido.",
    "A bowl of colourful sweets rests on a pedestal here, beside a small sign.":
        "Aquí, sobre un pedestal, descansa un cuenco de caramelos de colores, junto a un pequeño cartel.",
    "A rock sits on the floor near a stretch of spikes. Push it onto the switch to hold the spikes down, then cross while they are lowered.":
        "Una roca reposa en el suelo cerca de un tramo de pinchos. Empújala sobre el interruptor para mantener los pinchos bajados y cruza mientras están hundidos.",
    "A quiet room with holes worn in the floor and two stone plaques set into the walls, their old inscriptions waiting to be read.":
        "Una sala tranquila con agujeros desgastados en el suelo y dos placas de piedra encastradas en las paredes, con sus viejas inscripciones esperando a ser leídas.",
    "A large grey rock rests on the floor, and set into the ground nearby is a switch, a pressure plate. To move the rock, walk into it from the opposite side and it slides one space at a time. Push it onto the switch to open the way forward.":
        "Una gran roca gris reposa en el suelo, y encastrado en el suelo cerca hay un interruptor, una placa de presión. Para mover la roca, empújala caminando desde el lado opuesto y se deslizará un espacio cada vez. Empújala sobre el interruptor para abrir el camino.",
    "A small white ghost lies stretched across the path ahead, faint and half-see-through. The letters z, z, z drift up from him as he pretends to be asleep, though he does not look like he is fooling anyone.":
        "Un pequeño fantasma blanco yace tendido sobre el camino de delante, tenue y semitransparente. Las letras z, z, z flotan sobre él mientras finge estar dormido, aunque no parece estar engañando a nadie.",
    "A wedge of cheese sits on a table in the middle of the room, stuck fast after being left untouched for ages. In the far wall, a tiny mouse hole waits in hopeful silence.":
        "Una cuña de queso reposa sobre una mesa en el centro de la sala, pegada firmemente tras haber quedado intacta durante años. En la pared del fondo, una diminuta ratonera espera en un silencio esperanzado.",
    "A tall room. Just ahead, a hole in the floor drops down to a lower level. Down below there is a switch set into the floor, a couple of plump vegetable monsters half-buried in the earth, a faded ribbon lying on the ground, and a small, mournful ghost resting quietly in one of the hollows.":
        "Una sala alta. Justo delante, un agujero en el suelo desciende a un nivel inferior. Abajo hay un interruptor encastrado en el suelo, un par de rechonchos monstruos vegetales medio enterrados en la tierra, una cinta descolorida tirada en el suelo y un pequeño y afligido fantasma que descansa en silencio en uno de los huecos.",
    "A dim room dotted with tall pillars. Coloured switches - one red, one green, one blue - are set among them, and spikes block the way onward until the right ones are pressed.":
        "Una sala tenue salpicada de altos pilares. Entre ellos hay interruptores de colores (uno rojo, uno verde y uno azul), y unos pinchos bloquean el paso hasta que se pulsan los correctos.",
    "A small toy knife lies on the ground here, waiting to be picked up.":
        "Aquí, en el suelo, yace un pequeño cuchillo de juguete, esperando a que lo recojan.",
    "A large, leafless black tree stands in a small yard, its bare branches spread wide and dead leaves scattered all around. Just beyond it, a cosy home is built into the rock, warm light spilling from its windows.":
        "Un gran árbol negro sin hojas se alza en un pequeño patio, con sus ramas desnudas muy extendidas y hojas secas esparcidas por todas partes. Justo al otro lado, un acogedor hogar está construido en la roca, con una cálida luz derramándose por sus ventanas.",
    "You step inside Toriel's home, warm and snug after the cold stone of the Ruins. A staircase leads down to your left, and a hallway opens away to your right.":
        "Entras en el hogar de Toriel, cálido y acogedor tras la fría piedra de las Ruinas. Una escalera baja a tu izquierda, y un pasillo se abre a tu derecha.",
    "A cosy living room. A fire crackles in the hearth, and a large, cushioned reading chair sits beside it with a book left open on its arm. A doorway leads through to a small kitchen.":
        "Un acogedor salón. Un fuego crepita en el hogar, y junto a él hay un gran sillón de lectura acolchado con un libro abierto sobre el brazo. Una puerta da paso a una pequeña cocina.",
    "A long hallway lined with doors, softly lit. A mirror hangs on one wall and a little lamp glows in a corner. One of the doors has been made up as a bedroom, just for you.":
        "Un largo pasillo flanqueado de puertas, con una luz suave. Un espejo cuelga de una pared y una pequeña lámpara brilla en un rincón. Una de las puertas se ha preparado como dormitorio, solo para ti.",
    "A tall entrance hall of deep purple stone, where a wide staircase leads up and deeper into the Ruins. A patch of soft light marks a save point here.":
        "Un alto vestíbulo de entrada de piedra morada oscura, donde una amplia escalera sube y se adentra en las Ruinas. Un halo de luz suave marca aquí un punto de guardado.",
    "A room carpeted in a deep pile of dry red leaves that crinkle softly underfoot. A small frog-like monster rests by the wall, and a save point glows nearby. Passages lead away, one going up and one to the right.":
        "Una sala alfombrada por una espesa capa de hojas rojas y secas que crujen suavemente bajo tus pies. Un pequeño monstruo con aspecto de rana descansa junto a la pared, y cerca brilla un punto de guardado. De aquí salen pasajes, uno hacia arriba y otro hacia la derecha.",
    "A tall, narrow room with a hole in the floor that you can drop down through to the level below.":
        "Una sala alta y estrecha con un agujero en el suelo por el que puedes dejarte caer al nivel inferior.",
    "A room where a couple of small frog-like monsters rest by the walls. They will happily share advice, and a sign nearby explains how sparing a monster, pausing, and skipping through text all work.":
        "Una sala donde un par de pequeños monstruos con aspecto de rana descansan junto a las paredes. Con gusto te darán consejos, y un cartel cercano explica cómo funcionan perdonar a un monstruo, hacer una pausa y saltar el texto.",
    "A wider stone room where several passages meet.":
        "Una sala de piedra más amplia donde confluyen varios pasajes.",
    "A small frog-like monster sits quietly here, ready to offer a word of advice if you speak to it.":
        "Un pequeño monstruo con aspecto de rana está aquí sentado en silencio, dispuesto a ofrecerte un consejo si le hablas.",
    "Two spider webs are strung across a corner of this room, one small and one large, with a little sign beside them. It is a spider bake sale: leave a few coins in a web and the spiders will sell you a treat.":
        "Dos telarañas se extienden por un rincón de esta sala, una pequeña y una grande, con un pequeño cartel al lado. Es una venta benéfica de arañas: deja unas monedas en una telaraña y las arañas te venderán un dulce.",
    "A child's cosy bedroom. A neatly made bed sits beneath a soft lamp, with a box of toys and a few small comforts about the room. It has been made ready just for you.":
        "El acogedor dormitorio de un niño. Una cama bien hecha reposa bajo una lámpara suave, con una caja de juguetes y algunas pequeñas comodidades repartidas por la habitación. Se ha preparado solo para ti.",
    "Toriel's own bedroom, tidy and warm. A small chair sits in one corner, and there are a few of her personal things to read if you look around.":
        "El propio dormitorio de Toriel, ordenado y cálido. Una pequeña silla ocupa un rincón, y hay algunas de sus cosas personales que puedes leer si echas un vistazo.",
    "A long, cold basement corridor of grey stone, leading away from the warmth of the house above.":
        "Un largo y frío pasillo de sótano de piedra gris, que se aleja de la calidez de la casa de arriba.",
    "The cold stone corridor stretches on, quiet and dim.":
        "El frío pasillo de piedra continúa, silencioso y en penumbra.",
    "The passage narrows, the air growing colder the further you go.":
        "El pasaje se estrecha, y el aire se vuelve más frío cuanto más avanzas.",
    "A short stretch of corridor, the worn stone smooth underfoot.":
        "Un breve tramo de pasillo, con la piedra desgastada y lisa bajo tus pies.",
    "A very long, straight passage stretching far ahead into the cold. At its distant end stands a great doorway leading out of the Ruins.":
        "Un pasaje muy largo y recto que se extiende hacia el frío en la distancia. En su lejano extremo se alza una gran puerta que conduce fuera de las Ruinas.",

    # ===== ROOM AMBIANCE: Snowdin (batch 4) =====
    "The great stone door of the Ruins closes behind you, and ahead the path opens into a hushed, snow-covered forest. Bare trees crowd close on either side, and your breath fogs in the cold, still air.":
        "La gran puerta de piedra de las Ruinas se cierra a tu espalda, y ante ti el camino se abre a un bosque silencioso y cubierto de nieve. Árboles desnudos se apiñan a ambos lados, y tu aliento se empaña en el aire frío y quieto.",
    "A long path winding through snowy woods. A large branch lies fallen across the way, and further on stands a strange wooden gate, its bars set so far apart that anyone could simply walk between them. A conveniently shaped lamp sits off to one side.":
        "Un largo camino que serpentea entre bosques nevados. Una gran rama yace caída atravesando el paso, y más adelante se alza una extraña verja de madera, con los barrotes tan separados que cualquiera podría pasar sencillamente entre ellos. A un lado hay una lámpara de forma convenientemente oportuna.",
    "A clearing in the woods. A wooden sentry station, little more than a guard post of bars and a counter, stands beside the path. A save point glows softly nearby, next to a small sign.":
        "Un claro en el bosque. Una garita de vigilancia de madera, poco más que un puesto de guardia de barrotes y un mostrador, se alza junto al camino. Cerca brilla suavemente un punto de guardado, junto a un pequeño cartel.",
    "A small frozen pond tucked off the main path, its surface dotted with dark divots in the ice. A fishing rod has been left propped at the water's edge, its line trailing out across the frozen surface.":
        "Un pequeño estanque helado apartado del camino principal, con la superficie salpicada de oscuros hoyos en el hielo. Han dejado una caña de pescar apoyada en la orilla, con el sedal extendido sobre la superficie helada.",
    "A snowy stretch of path with another sentry station off to the side. A small, sharp-dressed bird monster loiters nearby, and telephone wires hum faintly overhead.":
        "Un tramo nevado del camino con otra garita de vigilancia a un lado. Cerca merodea un pequeño monstruo pájaro muy bien vestido, y unos cables de teléfono zumban débilmente por encima.",
    "A guard post built like a little dog house sits beside the path here. A dog treat lies within reach, and a small bell hangs at the counter.":
        "Aquí, junto al camino, hay un puesto de guardia construido como una pequeña caseta de perro. Una golosina para perros está al alcance, y una pequeña campana cuelga del mostrador.",
    "A wide, open stretch where a broad sheet of slippery ice covers much of the floor. Stepping onto the ice sends you sliding until you reach solid ground again. A sign stands near the path.":
        "Un tramo amplio y despejado donde una gran lámina de hielo resbaladizo cubre buena parte del suelo. Pisar el hielo te hace deslizarte hasta que vuelves a alcanzar suelo firme. Junto al camino hay un cartel.",
    "A quiet little clearing off the path. A small snowman stands here in the drifts, round and patient, as if waiting for someone to speak to him.":
        "Un pequeño y tranquilo claro apartado del camino. Aquí, entre la nieve amontonada, se alza un pequeño muñeco de nieve, redondo y paciente, como si esperara a que alguien le hablara.",
    "A snowy field crossed by an invisible maze of electric barriers. A trail of footprints has been left in the snow, marking out a safe path through it - follow the tracks to cross without being shocked.":
        "Un campo nevado atravesado por un laberinto invisible de barreras eléctricas. En la nieve han quedado unas huellas que marcan un camino seguro a través de él: sigue las pisadas para cruzar sin recibir una descarga.",
    "A broad snowfield opens up here. A vendor stands near the entrance selling frozen treats, and off across the snow a course has been marked out for a game of rolling a great snowball toward a distant hole. A lone tree stands at the edge of the clearing.":
        "Aquí se abre un amplio campo nevado. Cerca de la entrada, un vendedor ofrece golosinas heladas, y al otro lado de la nieve se ha marcado un recorrido para un juego que consiste en hacer rodar una gran bola de nieve hacia un agujero lejano. Un árbol solitario se alza al borde del claro.",
    "A small side clearing with a pair of dog houses standing side by side, a sign posted between them.":
        "Un pequeño claro lateral con un par de casetas de perro una junto a la otra, y un cartel clavado entre ambas.",
    "A snowy path where a large sheet of paper lies on the ground, printed with a word puzzle - a crossword, or perhaps a jumble - left behind mid-argument.":
        "Un camino nevado donde una gran hoja de papel yace en el suelo, con un pasatiempo de palabras impreso (un crucigrama, o quizá un revoltijo de letras), abandonado en plena discusión.",
    "A table has been set up in the snow, a plate of spaghetti frozen solid upon it beside a microwave that is not plugged into anything. A save point glows nearby, and a tiny mouse hole waits hopefully in the far wall.":
        "Han montado una mesa sobre la nieve, con un plato de espaguetis congelados sobre ella junto a un microondas que no está enchufado a nada. Cerca brilla un punto de guardado, y una diminuta ratonera espera esperanzada en la pared del fondo.",
    "A large puzzle room. Deep snow blankets the floor, with a bed of spikes set into it and tall trees dotted about. A safe path is hidden in the snow, and a sign nearby offers a clue to finding the way across.":
        "Una gran sala de puzles. Una espesa capa de nieve cubre el suelo, con un lecho de pinchos encastrado en ella y altos árboles repartidos por doquier. Un camino seguro está oculto en la nieve, y un cartel cercano ofrece una pista para encontrar el modo de cruzar.",
    "A puzzle room. Tiles marked with an X are set into the floor. Step on each one to switch it to an O; turn them all and the spikes ahead lower, opening the way. A sign nearby explains the rules.":
        "Una sala de puzles. En el suelo hay baldosas marcadas con una X. Pisa cada una para cambiarla a una O; conviértelas todas y los pinchos de delante bajan, abriendo el paso. Un cartel cercano explica las reglas.",
    "A larger version of the X and O puzzle, its floor covered in marked tiles and guarded by spikes. Step on each X to turn it into an O, clearing them all to lower the spikes and pass.":
        "Una versión más grande del puzle de X y O, con el suelo cubierto de baldosas marcadas y protegido por pinchos. Pisa cada X para convertirla en una O, y conviértelas todas para bajar los pinchos y pasar.",
    "A room whose floor is a grid of brightly coloured tiles, with a switch set into the wall on the far side that controls the puzzle. For all its complicated-sounding rules, the way ahead opens easily enough.":
        "Una sala cuyo suelo es una cuadrícula de baldosas de colores vivos, con un interruptor encastrado en la pared del fondo que controla el puzle. A pesar de sus reglas de apariencia complicada, el camino se abre con bastante facilidad.",
    "Another sentry station stands here, guarded by a dog in armour whose neck can stretch impossibly long. A save point glows nearby, and a dog house sits against the wall.":
        "Aquí se alza otra garita de vigilancia, custodiada por un perro con armadura cuyo cuello puede estirarse imposiblemente. Cerca brilla un punto de guardado, y una caseta de perro se apoya contra la pared.",
    "A small alcove where two lumpy snow sculptures have been built, rough likenesses of two skeletons. A hole in the floor here drops down to somewhere below.":
        "Una pequeña hornacina donde han construido dos toscas esculturas de nieve, imitaciones burdas de dos esqueletos. Un agujero en el suelo desciende aquí hacia algún lugar de abajo.",
    "A long, cavernous room of ice. Slippery patches send you sliding when you step on them, and holes gape in the floor ready to drop you to the level below. Pick a path carefully between them.":
        "Una sala larga y cavernosa de hielo. Las zonas resbaladizas te hacen deslizarte al pisarlas, y en el suelo se abren agujeros dispuestos a hacerte caer al nivel inferior. Elige con cuidado un camino entre ellos.",
    "The far end of the icy cavern, its walls glittering with frost. A shaggy, antlered monster stands off to one side, and the way out leads on toward warmer ground.":
        "El extremo más lejano de la caverna helada, con las paredes reluciendo de escarcha. A un lado se alza un monstruo peludo y con cornamenta, y la salida conduce hacia tierras más cálidas.",
    "A short passage leading out of the ice caves. Far off in the snowy distance, the tiny shape of a house can be seen, hinting at the town ahead.":
        "Un breve pasaje que sale de las cuevas de hielo. A lo lejos, en la distancia nevada, se distingue la diminuta silueta de una casa, que insinúa el pueblo que hay más adelante.",
    "A snowy hollow scattered with soft mounds of powder. A small dog house sits here, and a dog snuffles happily about among the drifts.":
        "Una hondonada nevada salpicada de blandos montículos de nieve en polvo. Aquí hay una pequeña caseta de perro, y un perro husmea feliz entre la nieve amontonada.",
    "A long rope bridge stretches across a deep, fog-filled chasm. An array of menacing contraptions - a dog, cannons, spears, and a flamethrower - has been rigged at the far end. Cross the bridge to go on.":
        "Un largo puente de cuerda se extiende sobre un profundo abismo lleno de niebla. En el extremo más lejano han montado un despliegue de amenazantes artilugios: un perro, cañones, lanzas y un lanzallamas. Cruza el puente para continuar.",
    "Snowdin town: a long, cheerful street of wooden buildings strung with coloured lights, a decorated tree glowing at its heart. Shops, an inn, and a warm-looking diner line the way, townsfolk milling about in the snow. At the far end stand two mailboxes and a house.":
        "El pueblo de Snowdin: una larga y alegre calle de edificios de madera engalanados con luces de colores, con un árbol decorado brillando en su centro. Tiendas, una posada y un restaurante de aspecto cálido flanquean el camino, y los vecinos deambulan por la nieve. En el extremo del fondo se alzan dos buzones y una casa.",
    "The quieter edge of town, past the last of the buildings. A wolf heaves blocks of ice one by one onto a conveyor that carries them off into the water, and a family of small slime monsters lingers nearby.":
        "El extremo más tranquilo del pueblo, más allá del último de los edificios. Un lobo arroja bloques de hielo uno a uno a una cinta transportadora que se los lleva hacia el agua, y una familia de pequeños monstruos de baba se entretiene cerca.",
    "A wooden dock at the water's edge, dark water lapping quietly against the boards. A strange, hooded boatman waits here, ready to carry you onward if you ask.":
        "Un embarcadero de madera a la orilla del agua, con las aguas oscuras lamiendo en silencio los tablones. Aquí espera un extraño barquero encapuchado, dispuesto a llevarte más lejos si se lo pides.",
    "The cosy lobby of the Snowdin inn. A front desk stands to one side, the innkeeper waiting behind it, ready to rent you a room for the night.":
        "El acogedor vestíbulo de la posada de Snowdin. A un lado hay un mostrador de recepción, con la posadera esperando detrás, dispuesta a alquilarte una habitación para pasar la noche.",
    "A snug little bedroom upstairs in the inn. A bed waits invitingly; a good rest here will mend your wounds.":
        "Una pequeña y acogedora habitación en el piso de arriba de la posada. Una cama espera de forma tentadora; un buen descanso aquí curará tus heridas.",
    "Grillby's, the town's warm and lively diner. Booths and tables fill the room, a crowd of dogs and other regulars gathered about, and behind the bar stands the owner - a quiet monster made of living fire.":
        "El Grillby's, el cálido y animado restaurante del pueblo. Reservados y mesas llenan el local, con un grupo de perros y otros clientes habituales reunidos, y tras la barra se encuentra el dueño: un monstruo callado hecho de fuego viviente.",
    "The town library - though the sign outside spells it librarby. Rows of shelves and reading tables fill the room, and a few studious monsters look up from their work as you enter.":
        "La biblioteca del pueblo, aunque el cartel de fuera la deletrea como bibloteca. Hileras de estanterías y mesas de lectura llenan la sala, y unos cuantos monstruos estudiosos levantan la vista de su trabajo cuando entras.",
    "A cluttered garage. A dog bed, a food bowl, and a well-chewed toy lie about the floor, and a strange barred contraption stands against one wall.":
        "Un garaje abarrotado. Una cama para perro, un cuenco de comida y un juguete bien mordisqueado están repartidos por el suelo, y un extraño artilugio con barrotes se apoya contra una pared.",
    "The front room of the skeleton brothers' house, cosy and lived-in. A couch faces a television, a kitchen opens off to the right, and doors lead away to the bedrooms.":
        "La sala principal de la casa de los hermanos esqueleto, acogedora y bien vivida. Un sofá mira hacia una televisión, una cocina se abre a la derecha, y unas puertas conducen a los dormitorios.",
    "A bedroom belonging to the taller brother. A race-car bed sits against one wall, along with a computer, a bookshelf, a box of bones, and an action figure posed on a small table.":
        "Un dormitorio que pertenece al hermano más alto. Una cama con forma de coche de carreras se apoya contra una pared, junto con un ordenador, una estantería, una caja de huesos y una figura de acción posando sobre una pequeña mesa.",
    "The shorter brother's bedroom, and a spectacular mess. Clothes and rubbish cover the floor, a self-sustaining tornado of trash spins quietly in one corner, and a treadmill sits buried and unused.":
        "El dormitorio del hermano más bajo, y un desorden espectacular. Ropa y basura cubren el suelo, un tornado de basura autosuficiente gira en silencio en un rincón, y una cinta de correr yace enterrada y sin usar.",
    "A wide, pitch-dark room. Something waits in the blackness ahead, just out of sight.":
        "Una sala amplia y completamente a oscuras. Algo aguarda en la negrura de delante, justo fuera de la vista.",
    "A hidden workshop behind a locked door, dusty and long forgotten. Odd papers are pinned to the walls, and a large object rests under a cloth in the corner.":
        "Un taller oculto tras una puerta cerrada con llave, polvoriento y largamente olvidado. Hay papeles extraños clavados en las paredes, y un gran objeto reposa bajo una tela en el rincón.",

    # ===== ROOM AMBIANCE: Waterfall (batch 5) =====
    "You leave the snow behind and step into Waterfall, a vast cavern of deep blue. Water trickles and echoes all around, glowing plants light the gloom, and the air turns warm and damp.":
        "Dejas atrás la nieve y entras en Waterfall, una vasta caverna de un azul intenso. El agua gotea y resuena por todas partes, plantas luminosas iluminan la penumbra, y el aire se vuelve cálido y húmedo.",
    "A quiet ledge where a wooden sentry station stands, unattended and suspiciously casual. A save point glows nearby, and a single blue echo flower grows here, softly repeating the last thing whispered to it.":
        "Un saliente tranquilo donde se alza una garita de vigilancia de madera, sin vigilar y sospechosamente informal. Cerca brilla un punto de guardado, y aquí crece una única flor de eco azul, que repite en voz baja lo último que le susurraron.",
    "A waterfall spills down one wall into a channel of flowing water. A large rock rests on the bank; push it into the stream to block the current so you can cross. A sign and an echo flower stand nearby.":
        "Una cascada se derrama por una pared hasta un canal de agua en movimiento. Una gran roca reposa en la orilla; empújala hacia la corriente para bloquearla y poder cruzar. Cerca hay un cartel y una flor de eco.",
    "A small alcove lit by a pair of tall, glowing blue mushrooms.":
        "Una pequeña hornacina iluminada por un par de altos hongos azules luminosos.",
    "A thick patch of tall grass grows in the middle of this room, high enough to hide in. On a ledge far above, a tall figure in armour stands watching, utterly still. A save point glows at the far side.":
        "Un espeso matorral de hierba alta crece en el centro de esta sala, lo bastante alto como para esconderse en él. En un saliente muy por encima, una alta figura con armadura observa, completamente inmóvil. Al otro lado brilla un punto de guardado.",
    "A bridge-seed puzzle. Clusters of seeds drift in the water; walk into them to push them together, and where they gather they sprout into solid lily-pad bridges you can cross. A sign nearby explains it.":
        "Un puzle de semillas-puente. Racimos de semillas flotan en el agua; camina hacia ellas para juntarlas, y allí donde se agrupan brotan puentes sólidos de nenúfares que puedes cruzar. Un cartel cercano lo explica.",
    "A larger stretch of water strewn with drifting bridge-seeds. Push the seeds together to grow lily-pad bridges across the water. Glowing mushrooms and a bell-shaped blossom light the room, and a sign offers a hint.":
        "Un tramo de agua más grande sembrado de semillas-puente a la deriva. Junta las semillas para hacer crecer puentes de nenúfares sobre el agua. Hongos luminosos y una flor con forma de campana iluminan la sala, y un cartel ofrece una pista.",
    "A small side room beside the water, lit by the soft glow of an echo flower.":
        "Una pequeña sala lateral junto al agua, iluminada por el suave resplandor de una flor de eco.",
    "A breathtaking room. The high ceiling glitters with countless tiny lights like a night sky full of stars, though they are only gems in the rock. Echo flowers grow here, each still whispering a wish that someone once breathed into it, and a telescope stands for gazing up.":
        "Una sala sobrecogedora. El alto techo reluce con incontables lucecitas, como un cielo nocturno cuajado de estrellas, aunque solo son gemas en la roca. Aquí crecen flores de eco, cada una susurrando aún un deseo que alguien exhaló en ella un día, y un telescopio invita a mirar hacia arriba.",
    "A wooden boardwalk over dark water, where a small, fussy water-loving monster busily scrubs the planks clean. A row of signs lines the path.":
        "Una pasarela de madera sobre aguas oscuras, donde un pequeño y quisquilloso monstruo amante del agua friega afanosamente los tablones. Una hilera de carteles bordea el camino.",
    "A long wooden boardwalk stretching out across open water.":
        "Una larga pasarela de madera que se extiende sobre aguas abiertas.",
    "A boardwalk with a tall patch of grass growing thick at its edge, just deep enough to hide in.":
        "Una pasarela con un matorral de hierba alta que crece espeso en su borde, justo lo bastante frondoso como para esconderse.",
    "A quiet nook with a save point, a glowing echo flower, and a little mouse hole worn in the wall.":
        "Un rincón tranquilo con un punto de guardado, una flor de eco luminosa y una pequeña ratonera desgastada en la pared.",
    "A dim room where motes of light drift through the air. A telescope stands here for looking out over the water, and a small sentry station sits nearby.":
        "Una sala en penumbra donde partículas de luz flotan en el aire. Aquí hay un telescopio para contemplar el agua, y cerca se encuentra una pequeña garita de vigilancia.",
    "A glowing-mushroom grotto where the Nice Cream vendor has set up his cart, selling frozen treats with kind words written on the wrappers.":
        "Una gruta de hongos luminosos donde el vendedor de Nice Cream ha montado su carrito, ofreciendo golosinas heladas con palabras amables escritas en los envoltorios.",
    "A vast, beautiful cavern glowing deep blue. Shining water pours down the walls, luminous plants drift and sway, and echo flowers whisper here and there. It is one of the loveliest sights in the whole Underground.":
        "Una vasta y hermosa caverna que brilla de un azul intenso. Agua reluciente se derrama por las paredes, plantas luminosas se mecen a la deriva, y flores de eco susurran aquí y allá. Es una de las vistas más bellas de todo el Subsuelo.",
    "A small clearing among glowing mushrooms, with a patch of tall grass at its heart.":
        "Un pequeño claro entre hongos luminosos, con un matorral de hierba alta en su centro.",
    "A ledge at the edge of a wide gap. A large, gentle bird waits here, willing to carry anyone small enough across to the far side.":
        "Un saliente al borde de un amplio abismo. Aquí espera un ave grande y apacible, dispuesta a llevar al otro lado a quien sea lo bastante pequeño.",
    "A long, shallow channel where the water has sunk worryingly low. A big, floppy, friendly sea creature lingers in the shallows, overjoyed to have someone to talk to.":
        "Un canal largo y poco profundo donde el agua ha bajado preocupantemente. Una gran criatura marina, blanda y amistosa, se entretiene en las aguas someras, encantada de tener con quien hablar.",
    "A rainy ledge where soft, sorrowful singing drifts on the air. A shy, fish-like monster hovers here, half-hiding behind her own song, and signs stand along the path.":
        "Un saliente lluvioso donde un canto suave y afligido flota en el aire. Aquí flota un tímido monstruo con aspecto de pez, medio escondido tras su propia canción, y hay carteles a lo largo del camino.",
    "A room with an old piano against the wall. Playing the right melody - the tune hinted at elsewhere in the caverns - will open the way to a hidden reward.":
        "Una sala con un viejo piano contra la pared. Tocar la melodía correcta (la tonada que se insinúa en otros puntos de las cavernas) abrirá el camino hacia una recompensa oculta.",
    "A hushed, shrine-like room. A curious old artifact rests here on a pedestal, waiting to be taken.":
        "Una sala silenciosa, como un santuario. Aquí reposa sobre un pedestal un curioso artefacto antiguo, esperando a que lo cojan.",
    "A stone statue sits alone in endless rain, silent and still. A box of umbrellas stands nearby; shelter the statue from the rain and it reveals the gentle music it was always meant to play.":
        "Una estatua de piedra permanece sola bajo una lluvia interminable, callada e inmóvil. Cerca hay una caja de paraguas; resguarda la estatua de la lluvia y revelará la dulce música que siempre estuvo destinada a tocar.",
    "The rain begins here. A box of umbrellas stands beside a sign - take one to stay dry on the long walk ahead.":
        "Aquí empieza la lluvia. Junto a un cartel hay una caja de paraguas: coge uno para no mojarte en la larga caminata que te espera.",
    "A long path through steady rain, waterfalls curtaining down on either side. It is a beautiful, wistful walk.":
        "Un largo camino bajo una lluvia constante, con cascadas cayendo como cortinas a ambos lados. Es un paseo hermoso y melancólico.",
    "A tall, rainy passage climbing upward, a single echo flower glowing softly along the way.":
        "Un pasaje alto y lluvioso que asciende, con una única flor de eco brillando suavemente por el camino.",
    "A rainy ledge with a wide, sweeping view: far across the water, the great grey castle of the King rises in the distance - the goal of the whole long journey.":
        "Un saliente lluvioso con una amplia vista panorámica: al otro lado del agua, a lo lejos, se alza el gran castillo gris del Rey, la meta de todo este largo viaje.",
    "A rainy path where a box of umbrellas waits to be refilled. A muscular, horse-like monster flexes and winks nearby.":
        "Un camino lluvioso donde una caja de paraguas espera a ser rellenada. Cerca, un musculoso monstruo con aspecto de caballo marca músculo y guiña un ojo.",
    "A quiet ledge in the rain, with a save point. The path ahead climbs onto a high wooden bridge.":
        "Un saliente tranquilo bajo la lluvia, con un punto de guardado. El camino de delante asciende a un alto puente de madera.",
    "A high, narrow bridge over a deep chasm, lashed with rain. This is dangerous ground - the armoured figure hunts here, hurling spears from the dark. Keep moving.":
        "Un puente alto y estrecho sobre un profundo abismo, azotado por la lluvia. Este es un terreno peligroso: la figura con armadura caza aquí, arrojando lanzas desde la oscuridad. No dejes de moverte.",
    "The far end of the bridge, hemmed in with nowhere left to run as spears rain down out of the gloom.":
        "El extremo más lejano del puente, acorralado y sin ningún sitio al que huir mientras las lanzas llueven desde la penumbra.",
    "A dim dump at the foot of the falls, where everything that tumbles into the Underground washes up. Heaps of garbage lie in shallow water, and a water-loving monster potters among them.":
        "Un vertedero en penumbra al pie de las cascadas, donde acaba todo lo que cae al Subsuelo. Montones de basura yacen en aguas someras, y un monstruo amante del agua trastea entre ellos.",
    "A patch of the garbage dump with a save point, standing in shallow water among the heaped rubbish.":
        "Una zona del vertedero con un punto de guardado, en medio de aguas someras entre la basura amontonada.",
    "A tall, junk-filled chamber. Among the piles of trash and a rusted old cooler floats a lumpy, furious dummy, spoiling for a fight.":
        "Una cámara alta y repleta de chatarra. Entre los montones de basura y una vieja nevera oxidada flota un muñeco grumoso y furioso, con ganas de pelea.",
    "A calmer stretch of Waterfall with a save point and a sign. Cosy little houses stand nearby, and a friendly clam-like monster chatters away by the path.":
        "Un tramo más tranquilo de Waterfall con un punto de guardado y un cartel. Cerca hay acogedoras casitas, y un amistoso monstruo con aspecto de almeja parlotea junto al camino.",
    "The yard outside a bright, fish-shaped house. A door waits to be knocked on - this is the home of the Underground's fierce captain of the guard.":
        "El patio ante una casa llamativa con forma de pez. Una puerta espera a que llamen: este es el hogar de la fiera capitana de la guardia del Subsuelo.",
    "The inside of Undyne's fish-shaped house, warm and full of character. A drawer stands against one wall, oddly stuffed with bones, and there are shelves and keepsakes to look over.":
        "El interior de la casa con forma de pez de Undyne, cálida y con mucha personalidad. Un cajón se apoya contra una pared, curiosamente repleto de huesos, y hay estanterías y recuerdos que curiosear.",
    "A quiet yard with a couple of small houses. A little white ghost drifts here, faint and shy.":
        "Un patio tranquilo con un par de casitas. Aquí flota un pequeño fantasma blanco, tenue y tímido.",
    "The ghost's home, small and a touch melancholy. A computer hums in the corner, a fridge stands nearby, and a stack of music CDs waits beside a spot on the floor where you can lie down and feel like garbage together.":
        "El hogar del fantasma, pequeño y algo melancólico. Un ordenador zumba en el rincón, cerca hay un frigorífico, y una pila de CD de música espera junto a un sitio en el suelo donde podéis tumbaros y sentiros como basura juntos.",
    "A neighbouring house, empty and quiet, its shelves lined with old diaries left behind by whoever once dreamed here.":
        "Una casa vecina, vacía y silenciosa, con las estanterías llenas de viejos diarios que dejó atrás quienquiera que soñó aquí una vez.",
    "A damp little snail farm. Snails inch slowly across their pen, and a small race track waits for anyone who fancies betting on the fastest one.":
        "Una pequeña y húmeda granja de caracoles. Los caracoles avanzan lentamente por su corral, y una pequeña pista de carreras espera a quien le apetezca apostar por el más rápido.",
    "A grassy ledge with a thick patch of tall grass and a few signs along the path.":
        "Un saliente cubierto de hierba, con un espeso matorral de hierba alta y unos cuantos carteles a lo largo del camino.",
    "A cosy shop carved into the rock, tended by a cheerful old turtle who has watched over the Underground's whole long history.":
        "Una acogedora tienda excavada en la roca, atendida por una alegre tortuga anciana que ha sido testigo de toda la larga historia del Subsuelo.",
    "A small dock at the water's edge, where the hooded boatman waits to ferry you onward if you wish.":
        "Un pequeño embarcadero a la orilla del agua, donde el barquero encapuchado espera para llevarte más lejos si lo deseas.",
    "A dark cavern flickering with glowing fireflies and drifting echoes, shallow water pooling across the floor.":
        "Una caverna oscura que titila con luciérnagas luminosas y ecos a la deriva, con aguas someras encharcadas por el suelo.",
    "A dark room lit only by clusters of glowing mushrooms. Stepping near a mushroom makes it flare bright, lighting the way forward from one to the next.":
        "Una sala oscura iluminada solo por racimos de hongos luminosos. Al acercarte a un hongo, este resplandece con fuerza, iluminando el camino de uno al siguiente.",
    "Temmie Village, a snug burrow full of excitable little cat-dog creatures who all chatter at once. There is a shop here, a save point, and a Temmie who will - for the right price - sell you the chance to pay for her college.":
        "La Aldea Temmie, una acogedora madriguera llena de pequeñas y excitables criaturas mitad gato mitad perro que parlotean todas a la vez. Aquí hay una tienda, un punto de guardado y una Temmie que, por el precio adecuado, te venderá la oportunidad de pagarle la universidad.",
    "A pitch-dark room. A lantern can be picked up and carried to light a small circle around you; glowing stones and other lanterns help mark the safe way through.":
        "Una sala completamente a oscuras. Se puede coger un farol y llevarlo para iluminar un pequeño círculo a tu alrededor; piedras luminosas y otros faroles ayudan a señalar el camino seguro.",
    "A darkened path with tall grass and shallow water. Ahead, the armoured figure smashes clean through a wall of blocks in furious pursuit.":
        "Un camino en penumbra con hierba alta y aguas someras. Más adelante, la figura con armadura atraviesa de un golpe una pared de bloques en furiosa persecución.",
    "A tall, glowing shaft lined with whispering echo flowers, a save point resting partway up. The flowers here carry an uneasy warning.":
        "Un pozo alto y luminoso flanqueado por flores de eco susurrantes, con un punto de guardado a media altura. Las flores de aquí transmiten una advertencia inquietante.",
    "A short, dim passage leading onward through the caverns.":
        "Un pasaje corto y en penumbra que sigue adelante a través de las cavernas.",
    "A small puzzle room, a switch box set into the wall.":
        "Una pequeña sala de puzles, con una caja de interruptores encastrada en la pared.",
    "Another bridge-seed puzzle, set amid a patch of tall grass. Push the drifting seeds together to sprout lily-pad bridges across the water.":
        "Otro puzle de semillas-puente, en medio de un matorral de hierba alta. Junta las semillas a la deriva para hacer brotar puentes de nenúfares sobre el agua.",
    "A small, quiet room with a sign to read and a curious mushroom-like monster.":
        "Una sala pequeña y tranquila con un cartel que leer y un curioso monstruo con aspecto de hongo.",
    "A lonely cliff-top bathed in golden light at the edge of Waterfall. The armoured figure makes her stand here, and there is nowhere left to run - only to turn and face her.":
        "Una solitaria cima de acantilado bañada en luz dorada al borde de Waterfall. La figura con armadura planta cara aquí, y ya no queda ningún sitio al que huir: solo darte la vuelta y hacerle frente.",
    "A path fleeing toward a wall of rising heat, the armoured figure giving relentless chase.":
        "Un camino que huye hacia un muro de calor creciente, con la figura con armadura persiguiéndote sin tregua.",
    "The border between Waterfall and Hotland, marked by a sign. The air here turns suddenly, punishingly hot.":
        "La frontera entre Waterfall y Hotland, marcada por un cartel. El aire aquí se vuelve de repente abrasadoramente caluroso.",

    # ===== ROOM AMBIANCE: Hotland + CORE (batch 6) =====
    "You arrive in Hotland. The air is a wall of dry heat, the rock glows red, and far below churns a river of lava. A sentry station stands nearby with its watchman fast asleep, and a water cooler offers a cup of cold water.":
        "Llegas a Hotland. El aire es un muro de calor seco, la roca brilla al rojo, y muy abajo se agita un río de lava. Cerca hay una garita de vigilancia con su centinela profundamente dormido, y un dispensador ofrece un vaso de agua fría.",
    "A hot, narrow ledge above the lava. A water cooler stands here, and a chatty clam-like monster lingers by the path.":
        "Un saliente caluroso y estrecho sobre la lava. Aquí hay un dispensador de agua, y un parlanchín monstruo con aspecto de almeja se entretiene junto al camino.",
    "A ledge before a large grey laboratory built into the rock, its door sealed shut. A save point glows nearby.":
        "Un saliente ante un gran laboratorio gris construido en la roca, con la puerta sellada. Cerca brilla un punto de guardado.",
    "A small dock at the lava's edge, where the hooded boatman waits to carry you onward if you wish.":
        "Un pequeño embarcadero a la orilla de la lava, donde el barquero encapuchado espera para llevarte más lejos si lo deseas.",
    "The dim interior of the Royal Scientist's laboratory. A gigantic monitor dominates one wall, and the room is cluttered with instruments, a fridge, and bags of dog food. It is unsettlingly quiet.":
        "El interior en penumbra del laboratorio de la Científica Real. Un monitor gigantesco domina una pared, y la sala está abarrotada de instrumentos, un frigorífico y sacos de comida para perros. Reina un silencio inquietante.",
    "A lower floor of the lab, escalators humming at either side. The shelves and cabinets here are packed with the scientist's not-so-secret collection of cartoons and figurines.":
        "Una planta inferior del laboratorio, con escaleras mecánicas zumbando a ambos lados. Las estanterías y vitrinas de aquí están repletas de la no tan secreta colección de dibujos animados y figuritas de la científica.",
    "A hot junction of red rock, pipes running along the ground and warning notices posted about.":
        "Un cruce caluroso de roca roja, con tuberías recorriendo el suelo y avisos de advertencia colgados por doquier.",
    "A tall shaft criss-crossed by moving conveyor belts, with jets of blue steam that puff you across the gaps. Ride the belts and the vents to climb it. A small volcano-like monster wanders here.":
        "Un pozo alto entrecruzado por cintas transportadoras en movimiento, con chorros de vapor azul que te impulsan por encima de los huecos. Súbete a las cintas y a los respiraderos para escalarlo. Aquí deambula un pequeño monstruo con aspecto de volcán.",
    "A room of steam vents. Stepping onto a vent launches you the way it blows; chain the jets together to cross the lava to the far side. A save point glows nearby.":
        "Una sala de respiraderos de vapor. Pisar un respiradero te lanza en la dirección en que sopla; encadena los chorros para cruzar la lava hasta el otro lado. Cerca brilla un punto de guardado.",
    "A smaller room of conveyor belts and a steam vent, a fiery bird-like monster hovering nearby.":
        "Una sala más pequeña de cintas transportadoras y un respiradero de vapor, con un ardiente monstruo con aspecto de pájaro flotando cerca.",
    "A laser puzzle. Beams of light bar the way: orange beams you must walk through while moving, blue beams only hurt if you move, so stand still to pass them. A switch at the end flips them all on or off.":
        "Un puzle de láseres. Haces de luz bloquean el paso: los haces naranjas debes atravesarlos en movimiento; los azules solo hacen daño si te mueves, así que quédate quieto para pasarlos. Un interruptor al final los enciende o apaga todos.",
    "A steam-vent room with a locked chip-card door in the middle. Ride the jets of steam across to reach the far side.":
        "Una sala de respiraderos de vapor con una puerta de tarjeta bloqueada en el centro. Cruza montado en los chorros de vapor para llegar al otro lado.",
    "A hot room lit by a tall beacon tower, gears turning in the walls and a blue laser barring part of the path.":
        "Una sala calurosa iluminada por una alta torre-faro, con engranajes girando en las paredes y un láser azul bloqueando parte del camino.",
    "A small hot room, a beacon tower glowing at its top and gears set into the walls.":
        "Una pequeña sala calurosa, con una torre-faro brillando en su cima y engranajes encastrados en las paredes.",
    "A shooting puzzle. A cannon sits at the bottom of the room and orange boxes float above; line the cannon up and fire to blast the boxes out of the way and clear the path.":
        "Un puzle de disparos. Un cañón se encuentra en la parte inferior de la sala y unas cajas naranjas flotan por encima; alinea el cañón y dispara para volar las cajas y despejar el camino.",
    "Another shooting puzzle - aim the cannon and fire to knock the floating orange boxes aside.":
        "Otro puzle de disparos: apunta con el cañón y dispara para apartar las cajas naranjas flotantes.",
    "A larger shooting puzzle, more orange boxes to blast clear with the cannon before you can pass.":
        "Un puzle de disparos más grande, con más cajas naranjas que volar con el cañón antes de poder pasar.",
    "A shooting puzzle - line up the cannon and fire to clear the floating orange boxes from your path.":
        "Un puzle de disparos: alinea el cañón y dispara para despejar de tu camino las cajas naranjas flotantes.",
    "A shooting puzzle set within the CORE, orange boxes to blast aside with the cannon.":
        "Un puzle de disparos situado dentro del CORE, con cajas naranjas que apartar con el cañón.",
    "A hot corner where jets of steam bounce you around the bend.":
        "Un rincón caluroso donde los chorros de vapor te hacen rebotar por la curva.",
    "A large steam-vent room, jets launching you rightward from ledge to ledge across the lava.":
        "Una gran sala de respiraderos de vapor, con chorros que te lanzan hacia la derecha de saliente en saliente por encima de la lava.",
    "A brightly-lit cooking-show stage, complete with a counter and dazzling studio lights. A robotic television star is putting on a live cooking programme here.":
        "Un escenario de programa de cocina muy iluminado, con su encimera y deslumbrantes luces de estudio. Aquí, una estrella robótica de la televisión está montando un programa de cocina en directo.",
    "A ledge with a save point and a sweeping view of the CORE, the great glowing engine of the Underground, rising in the distance.":
        "Un saliente con un punto de guardado y una amplia vista del CORE, el gran motor luminoso del Subsuelo, alzándose a lo lejos.",
    "A sentry station where a certain lazy skeleton sells hot dogs, a warm and greasy smell hanging in the air.":
        "Una garita de vigilancia donde cierto esqueleto perezoso vende perritos calientes, con un olor cálido y grasiento flotando en el aire.",
    "A hot branching path over the lava, warning notices posted along it and an aeroplane-like monster drifting nearby.":
        "Un caluroso camino que se bifurca sobre la lava, con avisos de advertencia colgados a lo largo y un monstruo con aspecto de avión flotando cerca.",
    "A small out-of-the-way room with an art-class sign on the wall.":
        "Una pequeña sala apartada con un cartel de clase de arte en la pared.",
    "A hot ledge where a wave-making monster lingers by the path.":
        "Un saliente caluroso donde un monstruo que hace olas se entretiene junto al camino.",
    "A conveyor-belt room with a row of three switches to set in the right pattern before the way opens.":
        "Una sala de cintas transportadoras con una fila de tres interruptores que hay que colocar en el patrón correcto antes de que se abra el paso.",
    "A puzzle of steam vents and conveyor belts. Bounce between the jets and ride the belts to shift the blocks and cross.":
        "Un puzle de respiraderos de vapor y cintas transportadoras. Rebota entre los chorros y súbete a las cintas para mover los bloques y cruzar.",
    "A hot room with a save point and a little mouse hole worn in the wall.":
        "Una sala calurosa con un punto de guardado y una pequeña ratonera desgastada en la pared.",
    "A hot room of steam vents, a fiery bird-like monster hanging about among the jets.":
        "Una sala calurosa de respiraderos de vapor, con un ardiente monstruo con aspecto de pájaro rondando entre los chorros.",
    "A studio set where the robotic television star is filming a live news report, lasers and steam vents rigged around the stage.":
        "Un plató donde la estrella robótica de la televisión está grabando un informativo en directo, con láseres y respiraderos de vapor montados alrededor del escenario.",
    "A ledge with another grand view of the CORE glowing in the distance.":
        "Un saliente con otra magnífica vista del CORE brillando a lo lejos.",
    "A cosy corner strung with spider webs, where a cheerful spider-girl runs a bake sale. Leave a few coins and the spiders will sell you a treat - all made for spiders, by spiders.":
        "Un acogedor rincón cubierto de telarañas, donde una alegre chica araña regenta una venta de dulces. Deja unas monedas y las arañas te venderán un dulce: todo hecho para arañas, por arañas.",
    "A large steam-vent maze climbing upward, jets of steam launching you from one ledge to the next.":
        "Un gran laberinto de respiraderos de vapor que asciende, con chorros de vapor que te lanzan de un saliente al siguiente.",
    "A room combining moving conveyor belts with blue laser beams - stay perfectly still through the blue beams while the belts carry you along. An echo flower glows to one side.":
        "Una sala que combina cintas transportadoras en movimiento con haces de láser azul: quédate perfectamente quieto al atravesar los haces azules mientras las cintas te arrastran. A un lado brilla una flor de eco.",
    "A hot little room lit by a beacon tower, a couple of gem-like child monsters loitering here.":
        "Una pequeña sala calurosa iluminada por una torre-faro, con un par de monstruos infantiles con aspecto de gema merodeando por aquí.",
    "A hot ledge strung with spider silk, a save point glowing softly here.":
        "Un saliente caluroso cubierto de seda de araña, con un punto de guardado brillando suavemente aquí.",
    "A long, web-draped corridor deep in spider territory. Sticky silk slows your steps, spiders watch from above, and their mistress is not far off.":
        "Un largo pasillo cubierto de telarañas en lo profundo del territorio de las arañas. La seda pegajosa ralentiza tus pasos, las arañas observan desde lo alto, y su señora no anda lejos.",
    "A small room with a sign to read and a monster pacing about.":
        "Una pequeña sala con un cartel que leer y un monstruo que se pasea de un lado a otro.",
    "A huge coloured-tile puzzle: a long floor of tiles in many colours, each colour carrying its own rule - some safe, some not - which a voice calls out before you cross. A little volcano-monster waits nearby.":
        "Un enorme puzle de baldosas de colores: un largo suelo de baldosas de muchos colores, cada color con su propia regla (algunos seguros, otros no) que una voz anuncia antes de que cruces. Cerca espera un pequeño monstruo volcán.",
    "The grand entrance to the MTT Resort, a glitzy hotel carved out over the lava. The Nice Cream vendor has set up his cart by the doors.":
        "La gran entrada del MTT Resort, un ostentoso hotel excavado sobre la lava. El vendedor de Nice Cream ha montado su carrito junto a las puertas.",
    "The approach to the hotel doors, plush red carpet underfoot.":
        "La entrada hacia las puertas del hotel, con una mullida alfombra roja bajo tus pies.",
    "The opulent lobby of the MTT Resort, gaudy and gold. A fountain shaped like the robotic star bubbles in the centre, a receptionist waits at the desk, and well-dressed monster guests mill about. A save point glows nearby, and an elevator stands ready.":
        "El opulento vestíbulo del MTT Resort, chillón y dorado. Una fuente con la forma de la estrella robótica borbotea en el centro, un recepcionista espera en el mostrador, y elegantes monstruos huéspedes deambulan por allí. Cerca brilla un punto de guardado, y un ascensor aguarda listo.",
    "The hotel's fancy restaurant, tables set with care and potted plants along the walls, guests dining quietly.":
        "El elegante restaurante del hotel, con las mesas puestas con esmero y macetas a lo largo de las paredes, y huéspedes cenando en silencio.",
    "A hallway of guest-room doors, a weary slime janitor mopping the floor.":
        "Un pasillo de puertas de habitaciones, con un fatigado conserje de baba fregando el suelo.",
    "A plush hotel bedroom, a large soft bed waiting - a rest here will restore you.":
        "Una lujosa habitación de hotel, con una gran cama blanda esperando: un descanso aquí te restablecerá.",
    "A dark shaft leading down toward the CORE, the hum of heavy machinery rising from below.":
        "Un pozo oscuro que desciende hacia el CORE, con el zumbido de la maquinaria pesada subiendo desde abajo.",
    "The entrance to the CORE, the Underground's vast power plant. Dark metal walls glow with strips of blue light, the air thrumming with energy. An elevator stands here.":
        "La entrada al CORE, la vasta central eléctrica del Subsuelo. Las oscuras paredes de metal brillan con tiras de luz azul, y el aire vibra de energía. Aquí hay un ascensor.",
    "A CORE chamber lit an eerie blue, small flames flickering in braziers along the walls.":
        "Una cámara del CORE iluminada de un azul inquietante, con pequeñas llamas titilando en braseros a lo largo de las paredes.",
    "A CORE room lined with glowing totems, a great door set in the wall ahead.":
        "Una sala del CORE flanqueada por tótems luminosos, con una gran puerta en la pared del fondo.",
    "A CORE room barred by laser beams, a switch nearby to toggle them - walk through the orange, stand still for the blue.":
        "Una sala del CORE bloqueada por haces de láser, con un interruptor cerca para alternarlos: atraviesa los naranjas, quédate quieto para los azules.",
    "A small CORE chamber glowing with totems and blue light.":
        "Una pequeña cámara del CORE que brilla con tótems y luz azul.",
    "A small CORE chamber where a shadowy foe lurks in the blue light.":
        "Una pequeña cámara del CORE donde un enemigo sombrío acecha en la luz azul.",
    "A long CORE hall crossed by a gauntlet of blue and orange laser beams. Walk through the orange, freeze still for the blue.":
        "Un largo salón del CORE atravesado por una sucesión de haces de láser azules y naranjas. Atraviesa los naranjas, quédate inmóvil para los azules.",
    "A CORE junction with a save point and signs, glowing light-strips marking several ways onward. It is easy to get turned around in here.":
        "Un cruce del CORE con un punto de guardado y carteles, con tiras de luz brillantes señalando varios caminos. Es fácil desorientarse aquí.",
    "A CORE corridor with conveyor belts running along the floor and blue light-strips glowing on the dark walls.":
        "Un pasillo del CORE con cintas transportadoras recorriendo el suelo y tiras de luz azul brillando en las oscuras paredes.",
    "A CORE corridor branching off to the left, glowing totems set in the walls.":
        "Un pasillo del CORE que se bifurca hacia la izquierda, con tótems luminosos encastrados en las paredes.",
    "A CORE corridor junction, blue light-strips lining the dark metal walls.":
        "Un cruce de pasillos del CORE, con tiras de luz azul flanqueando las oscuras paredes de metal.",
    "A CORE corridor near the top of the maze, signs posted and light-strips glowing.":
        "Un pasillo del CORE cerca de lo alto del laberinto, con carteles colgados y tiras de luz brillando.",
    "A CORE corridor junction humming with power, blue light running along the walls.":
        "Un cruce de pasillos del CORE que zumba de energía, con luz azul recorriendo las paredes.",
    "A CORE corridor where a shimmering force-field seals one of the doors.":
        "Un pasillo del CORE donde un reluciente campo de fuerza sella una de las puertas.",
    "A CORE corner where a walkway bridges the dark drop, glowing totems standing guard.":
        "Un recodo del CORE donde una pasarela salva la oscura caída, con tótems luminosos montando guardia.",
    "The central CORE junction, paths branching in every direction, a shadowy foe prowling nearby.":
        "El cruce central del CORE, con caminos que se bifurcan en todas direcciones y un enemigo sombrío merodeando cerca.",
    "A CORE alcove off the maze, holding something worth taking and a monster to talk to.":
        "Una hornacina del CORE apartada del laberinto, que alberga algo que merece la pena coger y un monstruo con quien hablar.",
    "A CORE alcove tucked away off the maze, holding a small reward.":
        "Una hornacina del CORE escondida fuera del laberinto, que alberga una pequeña recompensa.",
    "A CORE hall where a fierce warrior monster blocks the way, a switch glinting at the far end.":
        "Un salón del CORE donde un fiero monstruo guerrero bloquea el paso, con un interruptor reluciendo en el extremo del fondo.",
    "A long CORE bridge spanning a dark chasm, glowing totems lining the rails and light streaming overhead.":
        "Un largo puente del CORE que cruza un oscuro abismo, con tótems luminosos flanqueando las barandillas y luz derramándose por encima.",
    "A CORE chamber with a save point and a great door ahead, an elevator waiting to one side.":
        "Una cámara del CORE con un punto de guardado y una gran puerta delante, con un ascensor esperando a un lado.",
    "A tall, dramatic CORE stage - the setting for a grand confrontation with the robotic star.":
        "Un alto y dramático escenario del CORE: el escenario de una gran confrontación con la estrella robótica.",
    "The far end of the CORE, an elevator waiting to carry you up out of the depths.":
        "El extremo más lejano del CORE, con un ascensor esperando para llevarte arriba, fuera de las profundidades.",
    "A small elevator car, its control panel glowing beside the door.":
        "Una pequeña cabina de ascensor, con su panel de control brillando junto a la puerta.",
    "A small elevator car, the floor number glowing on a sign by the door.":
        "Una pequeña cabina de ascensor, con el número de planta brillando en un cartel junto a la puerta.",
    "A dim elevator that has carried you somewhere you were never meant to go.":
        "Un ascensor en penumbra que te ha llevado a un lugar al que nunca debías ir.",

    # ===== ROOM AMBIANCE: True Lab (batch 6) =====
    "A dark landing outside the elevator, a heavy lab door standing ahead.":
        "Un rellano oscuro a la salida del ascensor, con una pesada puerta de laboratorio delante.",
    "A dark laboratory hall, its walls lined with humming monitors that cast a sickly glow.":
        "Un oscuro pasillo de laboratorio, con las paredes cubiertas de monitores zumbantes que arrojan un resplandor enfermizo.",
    "A shadowy hub where several lab doors meet. A save point glows here, dark withered plants stand in their pots, and a torn note lies on the floor.":
        "Un sombrío nudo donde confluyen varias puertas de laboratorio. Aquí brilla un punto de guardado, hay oscuras plantas marchitas en sus macetas, y una nota rasgada yace en el suelo.",
    "A short, dark lab corridor, a monitor flickering on the wall.":
        "Un corto y oscuro pasillo de laboratorio, con un monitor parpadeando en la pared.",
    "A grim operating room half-lost in fog, empty tables and grimy sinks lined up in the gloom.":
        "Un lúgubre quirófano medio perdido en la niebla, con mesas vacías y pilas mugrientas alineadas en la penumbra.",
    "A dark room with a coloured lever on the wall - one of several to be set - and a torn note lying nearby.":
        "Una sala oscura con una palanca de color en la pared (una de varias que hay que accionar) y una nota rasgada tirada cerca.",
    "A dark room with a coloured lever on the wall and a torn note on the floor.":
        "Una sala oscura con una palanca de color en la pared y una nota rasgada en el suelo.",
    "A dark room with a coloured lever and a torn note lying beside it.":
        "Una sala oscura con una palanca de color y una nota rasgada tirada a su lado.",
    "A dark corridor lined with glowing monitors.":
        "Un oscuro pasillo flanqueado por monitores luminosos.",
    "A dim dormitory of empty beds shrouded in fog. Something watches from among them. A save point glows in one corner, and a key rests on one of the beds.":
        "Un dormitorio en penumbra con camas vacías envueltas en niebla. Algo observa desde entre ellas. En un rincón brilla un punto de guardado, y una llave reposa sobre una de las camas.",
    "A long dark hall lined with mirrors. In the fog at the far end, a strange, shifting shape waits and watches.":
        "Un largo y oscuro salón flanqueado por espejos. En la niebla del extremo del fondo, una extraña forma cambiante espera y observa.",
    "Another dark lab corridor, monitors glowing faintly along the walls.":
        "Otro oscuro pasillo de laboratorio, con monitores brillando débilmente a lo largo de las paredes.",
    "A small room with a drawn shower curtain, something lurking behind it.":
        "Una pequeña sala con una cortina de ducha corrida, con algo acechando tras ella.",
    "A dark room built around a strange extraction machine. A save point glows here - though this one seems oddly, unsettlingly alive.":
        "Una sala oscura construida en torno a una extraña máquina de extracción. Aquí brilla un punto de guardado, aunque este parece rara e inquietantemente vivo.",
    "A dark room with an old television set and a coloured lever, a torn note on the floor.":
        "Una sala oscura con un viejo televisor y una palanca de color, con una nota rasgada en el suelo.",
    "A cold storage room, ranks of dark fridges humming beneath whirring fans. Something stirs among them.":
        "Una cámara frigorífica, con hileras de oscuros frigoríficos zumbando bajo ventiladores en marcha. Algo se agita entre ellos.",
    "A room walled with great spinning fans, fog curling between them. A shape moves in the mist.":
        "Una sala amurallada con grandes ventiladores giratorios, con la niebla enroscándose entre ellos. Una forma se mueve en la bruma.",
    "A dark corridor of monitors leading toward the power room.":
        "Un oscuro pasillo de monitores que conduce hacia la sala de energía.",
    "A small room holding the main power switch, ready to bring the lights back on.":
        "Una pequeña sala que alberga el interruptor principal de energía, listo para volver a encender las luces.",
    "An elevator ready to carry you up out of the true lab.":
        "Un ascensor listo para llevarte arriba, fuera del verdadero laboratorio.",

    # ===== ROOM AMBIANCE: New Home (batch 6) =====
    "You step out of the elevator into New Home, the King's grey city high above. A save point glows nearby, and a slow, mournful music hangs in the air.":
        "Sales del ascensor a New Home, la gris ciudad del Rey en las alturas. Cerca brilla un punto de guardado, y una música lenta y afligida flota en el aire.",
    "A long grey approach toward the castle, the ruins of an old, old home looming quietly ahead.":
        "Una larga y gris aproximación hacia el castillo, con las ruinas de un hogar muy, muy antiguo asomando en silencio más adelante.",
    "The front of the castle, tall and grey and silent. A save point glows here.":
        "La fachada del castillo, alta, gris y silenciosa. Aquí brilla un punto de guardado.",
    "A tidy little kitchen. A freshly baked pie rests on the counter, filling the still air with a sweet and sorrowful smell.":
        "Una pequeña y ordenada cocina. Un pastel recién horneado reposa sobre la encimera, llenando el aire quieto de un olor dulce y triste.",
    "You step into a home almost exactly like Toriel's - the same shape, the same rooms - but grey, hushed, and long abandoned, everything left just as it was. Stairs lead down from the entrance hall.":
        "Entras en un hogar casi idéntico al de Toriel (la misma forma, las mismas habitaciones), pero gris, silencioso y largamente abandonado, con todo tal como estaba. Unas escaleras bajan desde el vestíbulo de entrada.",
    "A living room that echoes Toriel's exactly: a reading chair beside a cold hearth, a dining table set for a family. But it is dim and empty now, coated in quiet dust.":
        "Un salón que es un calco exacto del de Toriel: un sillón de lectura junto a un hogar apagado, una mesa de comedor puesta para una familia. Pero ahora está en penumbra y vacío, cubierto de un polvo silencioso.",
    "A hallway like the one in Toriel's home, a mirror hanging on the wall. The bedrooms off this hall hold the belongings of children who are long gone.":
        "Un pasillo como el del hogar de Toriel, con un espejo colgado en la pared. Los dormitorios que dan a este pasillo guardan las pertenencias de unos niños que hace mucho que no están.",
    "A long grey corridor lined with plaques. Each one, as you pass, tells another piece of the Underground's sad and ancient history.":
        "Un largo pasillo gris flanqueado por placas. Cada una, al pasar, cuenta otro fragmento de la triste y antigua historia del Subsuelo.",
    "A vast hall of golden light, tall pillars throwing long shadows and sunlight streaming through great windows. A save point glows near the entrance, and far off at the other end, a figure waits to weigh the journey you have made.":
        "Un vasto salón de luz dorada, con altos pilares proyectando largas sombras y la luz del sol entrando a raudales por grandes ventanales. Cerca de la entrada brilla un punto de guardado, y a lo lejos, en el otro extremo, una figura espera para sopesar el viaje que has hecho.",
    "A quiet grey chamber with a save point, the way narrowing toward the throne room ahead.":
        "Una tranquila cámara gris con un punto de guardado, con el camino estrechándose hacia la sala del trono que hay delante.",
    "A solemn room lined with child-sized coffins, each one carefully made and marked with a name.":
        "Una sala solemne flanqueada por ataúdes del tamaño de un niño, cada uno cuidadosamente elaborado y marcado con un nombre.",
    "The throne room, carpeted with golden flowers that glow in the light pouring from above. Two thrones stand here, one of them draped and unused. This is where the King waits. A save point glows nearby.":
        "La sala del trono, alfombrada de flores doradas que brillan bajo la luz que se derrama desde lo alto. Aquí se alzan dos tronos, uno de ellos cubierto y sin usar. Aquí es donde espera el Rey. Cerca brilla un punto de guardado.",
    "A solemn grey chamber just before the barrier, a save point glowing softly here.":
        "Una solemne cámara gris justo antes de la barrera, con un punto de guardado brillando suavemente aquí.",
    "The barrier itself: a towering wall of blinding white light, the ancient magic that seals the whole Underground away from the world above.":
        "La barrera en sí: un imponente muro de cegadora luz blanca, la antigua magia que sella todo el Subsuelo, apartándolo del mundo de la superficie.",
    "A passage leading up and onward, toward the surface at last.":
        "Un pasaje que conduce hacia arriba y adelante, hacia la superficie por fin.",

    # ===== leftover cutscene beats (Ruins / Undyne / ghost / Toriel goodbye) =====
    "Flowey's cheerful face suddenly twists into something cruel. The pellets turn on you, forming a ring around your soul that closes in to surround you.":
        "La alegre cara de Flowey se retuerce de pronto en algo cruel. Los perdigones se vuelven contra ti, formando un anillo alrededor de tu alma que se cierra para rodearte.",
    "A ball of fire streaks in and strikes the flower, knocking it clean out of sight. A tall, motherly monster steps out of the shadows toward you.":
        "Una bola de fuego llega surcando el aire y golpea a la flor, quitándola de en medio de un golpe. Un monstruo alto y maternal sale de las sombras hacia ti.",
    "Toriel stands blocking the great stone door, and flames bloom in her hands. Yet each time she attacks, her fire curves away from you at the last moment. She cannot bring herself to truly hurt you.":
        "Toriel permanece bloqueando la gran puerta de piedra, y las llamas florecen en sus manos. Sin embargo, cada vez que ataca, su fuego se desvía de ti en el último momento. No es capaz de hacerte daño de verdad.",
    "The path ahead is dark and silent. Behind you, soft footsteps approach, and a short, shadowy figure steps up close and holds out a hand toward you.":
        "El camino de delante está oscuro y silencioso. A tu espalda, unos pasos suaves se acercan, y una figura baja y sombría se aproxima y te tiende una mano.",
    "A spear of blue light slams into the ground beside you. High on the ledge above stands a tall armoured figure, glaring down at you through a horned helmet.":
        "Una lanza de luz azul se clava en el suelo junto a ti. En lo alto del saliente de arriba se yergue una alta figura con armadura, fulminándote con la mirada a través de un yelmo con cuernos.",
    "You crouch hidden in the tall grass. Heavy armoured footsteps approach and stop right beside you. A gauntleted hand reaches down, closes around the small monster child at your side, lifts him up by the head - then sets him down and strides away.":
        "Te agachas escondido entre la hierba alta. Unos pesados pasos con armadura se acercan y se detienen justo a tu lado. Una mano enguantada desciende, se cierra en torno al pequeño monstruo infantil que tienes al lado, lo levanta por la cabeza y luego lo deja en el suelo y se marcha a grandes zancadas.",
    "The armoured warrior drops onto the bridge behind you and gives chase, hurling spear after spear of blue light as you run.":
        "La guerrera con armadura cae sobre el puente a tu espalda y te persigue, arrojando lanza tras lanza de luz azul mientras corres.",
    "Cornered at the end of the bridge, you can only back away as she advances - until the walkway gives out beneath you and you plunge down into the dark.":
        "Acorralado al final del puente, solo puedes retroceder mientras ella avanza, hasta que la pasarela cede bajo tus pies y te precipitas hacia la oscuridad.",
    "Overcome by the sweltering heat inside her heavy armour, the warrior sways, staggers, and at last crashes to the ground, where she lies still.":
        "Vencida por el calor sofocante dentro de su pesada armadura, la guerrera se tambalea, trastabilla y por fin se desploma contra el suelo, donde queda inmóvil.",
    "The little ghost slowly fades, growing fainter and fainter, until he has vanished from sight.":
        "El pequeño fantasma se desvanece lentamente, volviéndose cada vez más tenue, hasta que desaparece de la vista.",
    "Toriel wraps you in a warm embrace, holding you close for a long moment. Then she lets go, turns away, and walks off down the corridor, leaving you to go on alone.":
        "Toriel te envuelve en un cálido abrazo, estrechándote contra ella durante un largo instante. Luego te suelta, se da la vuelta y se aleja por el pasillo, dejándote continuar solo.",

    # ===== leftover doubled-form UI / dialogue-prompt fragments =====
    "There is no description for this area.":
        "No hay descripción para esta zona.",
    "name empty":
        "nombre vacío",
    " interactables. Nearest: ":
        " objetos interactivos. El más cercano: ",
    ", spareable":
        ", se puede perdonar",
    " damage.":
        " de daño.",
    " gold. ":
        " de oro. ",
    " of 8 inventory slots free.":
        " de 8 espacios de inventario libres.",
    " exits. ":
        " salidas. ",
    ". Attack ":
        ". Ataque ",
    ". Defense ":
        ". Defensa ",
    ". Gold ":
        ". Oro ",
    ". Next ":
        ". Siguiente ",
    " Alphys hints ":
        " pistas de Alphys ",
    " Press 1 to 4, then Z to lock in.":
        " Pulsa del 1 al 4 y luego Z para confirmar.",
    ". Press Z to lock in.":
        ". Pulsa Z para confirmar.",
    " of 6 freed. Your attacks hit harder now.":
        " de 6 liberadas. Tus ataques golpean más fuerte ahora.",
    " of 6 souls freed. HP ":
        " de 6 almas liberadas. PV ",
    ". Press left or right, then Z. On ":
        ". Pulsa izquierda o derecha y luego Z. En ",
    ". Press Z.":
        ". Pulsa Z.",
    " percent":
        " por ciento",
    # ===== v1.6 batch: round-2 fixes, Mettaton minigames, exits key, shop sell, storage box =====
    'A bridge-seed puzzle. Clusters of seeds drift in the water and sprout into lily-pad bridges where they gather, but lining them up by sight is impractical without vision, so this one is best skipped. To skip it: press P to hear the exits, press P again to cycle between them, then press O to go through.':
        'Un puzle de semillas puente. Grupos de semillas van a la deriva por el agua y brotan formando puentes de nenúfares allí donde se juntan, pero alinearlas a simple vista no es práctico sin visión, así que es mejor saltarse este. Para saltarlo: pulsa P para oír las salidas, pulsa P otra vez para pasar de una a otra y luego pulsa O para cruzar.',
    'A puzzle of steam vents and conveyor belts. The bounce pads and belts move you automatically, so this one is a visual puzzle best skipped. To skip it: press P to hear the exits, press P again to cycle between them, then press O to go through. The way onward is the door to the east.':
        'Un puzle de chorros de vapor y cintas transportadoras. Las plataformas de rebote y las cintas te mueven automáticamente, así que este es un puzle visual que conviene saltarse. Para saltarlo: pulsa P para oír las salidas, pulsa P otra vez para pasar de una a otra y luego pulsa O para cruzar. El camino a seguir es la puerta del este.',
    'No hint for what you are doing right now.':
        'No hay ninguna ayuda para lo que estás haciendo ahora mismo.',
    'No way out found in this room.':
        'No se ha encontrado ninguna salida en esta sala.',
    ' paces. Press V to walk there, or U for the next one.':
        ' pasos. Pulsa V para ir hasta allí, o U para la siguiente.',
    'Bomb, walk here to defuse':
        'Bomba, ven aquí para desactivarla',
    'Already sold':
        'Ya vendido',
    ', she will not buy this':
        ', no te comprará esto',
    'Yes, sell for ':
        'Sí, vender por ',
    'Carrying. ':
        'Llevas. ',
    ' seconds':
        ' segundos',
    'Halfway up.':
        'Vas por la mitad.',
    'Almost at the top.':
        'Ya casi estás arriba.',
    'You reached the top!':
        '¡Has llegado arriba!',
    'Out of time.':
        'Se acabó el tiempo.',
    'You smell of oranges. Water bites now.':
        'Hueles a naranjas. Ahora el agua muerde.',
    'You smell of lemons. Water is safe now.':
        'Hueles a limones. Ahora el agua es segura.',
    'Ouch, piranhas!':
        '¡Ay, pirañas!',
    'Yellow, zap':
        'Amarillo, descarga',
    'Water, danger':
        'Agua, peligro',
    'the exit':
        'la salida',
    'pink, safe':
        'rosa, seguro',
    'red, blocked':
        'rojo, bloqueado',
    'purple, lemons':
        'morado, limones',
    'yellow, zap':
        'amarillo, descarga',
    'orange scent':
        'olor a naranja',
    'water, piranhas':
        'agua, pirañas',
    'water, safe':
        'agua, seguro',
    'You crossed the tiles! Well done.':
        '¡Has cruzado las baldosas! Bien hecho.',
    'Bomb defused. ':
        'Bomba desactivada. ',
    'All bombs defused! Well done.':
        '¡Todas las bombas desactivadas! Bien hecho.',
    'north':
        'norte',
    'south':
        'sur',
    'west':
        'oeste',
    'east':
        'este',
    'Way out ':
        'Salida ',
    ', on':
        ', activado',
    ', off':
        ', desactivado',
    'Menu. ':
        'Menú. ',
    ' plus ':
        ' más ',
    ', or ':
        ', o ',
    ' for ':
        ' por ',
    'Sell ':
        'Vender ',
    'Sell. ':
        'Vender. ',
    ' gold':
        ' de oro',
    ' gold? ':
        ' de oro? ',
    ' left.':
        ' restantes.',
    'edge':
        'borde',
    'green':
        'verde',
    'Pink':
        'Rosa',
    'Water':
        'Agua',
    'Green':
        'Verde',
    'Left':
        'Izquierda',
    'Right':
        'Derecha',
    'Up':
        'Arriba',
    'Down':
        'Abajo',

}

beats_set = set(l for l in io.open("_corpus_beats.txt", encoding="utf-8").read().split("\n") if l)

text = io.open(SRC, encoding="utf-8").read()
merged = dict(T_ui); merged.update(T_ad)

applied_dq = applied_sq = 0
for k in sorted(merged, key=len, reverse=True):
    v = merged[k]
    dq = '""' + k + '""'
    if dq in text:
        text = text.replace(dq, '""' + v + '""'); applied_dq += 1
    if k in beats_set:
        sq = '"' + k + '"'
        if sq in text:
            text = text.replace(sq, '"' + v + '"'); applied_sq += 1

# --- SANITIZER TWEAK: ArceUseless's Spanish dialogue uses '#' as its line break where English
#     uses '&'. Teach every speech sanitizer to treat '#' exactly like '&' so Spanish dialogue is
#     spoken cleanly (mirrors the old shipped ES build, extended to the mod's newer sanitizer sites).
POST = [
    ('if (_c == ""&"")', 'if (_c == ""&"" || _c == ""#"")'),
    ('string_pos(""&"", _s)', 'string_pos(""&"", string_replace_all(_s, ""#"", ""&""))'),
    ('_qt = string_replace_all(_qt, ""&"", "" "");', '_qt = string_replace_all(_qt, ""&"", "" ""); _qt = string_replace_all(_qt, ""#"", "" "");'),
    ('c == ""&"" || c == ""/""', 'c == ""&"" || c == ""#"" || c == ""/""'),
    ('ec == ""&"" || ci', 'ec == ""&"" || ec == ""#"" || ci'),
    ('bc == ""&"" || bc == ""/""', 'bc == ""&"" || bc == ""#"" || bc == ""/""'),
    ('bec == ""&"" || bci', 'bec == ""&"" || bec == ""#"" || bci'),
]
post_applied = 0
for find, repl in POST:
    n = text.count(find)
    if n == 0:
        print("  !! POST pattern NOT FOUND:", find)
    else:
        text = text.replace(find, repl); post_applied += n

io.open(OUT, "w", encoding="utf-8").write(text)
print("sanitizer '#' patterns applied:", post_applied, "/", len(POST))
print("doubled-form keys applied:", applied_dq)
print("single-form (beat) keys applied:", applied_sq)
print("total merged keys:", len(merged), "(existing", len(T_ui), "+ new", len(T_ad), ")")

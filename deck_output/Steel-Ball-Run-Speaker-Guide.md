# Steel Ball Run · Speaker guide

**Rowaan & Rohan · ~7 minutes of slides + 4–5 minutes of live demo**

Anything in brackets is a cue for you. The timings are checkpoints. You can paraphrase the spoken lines and pause on the images as needed.

## 1. Steel Ball Run · 0:00–0:35

[Start on the cover. Give the audience a moment to look at the street.]

“Hi, I’m Rowaan. This is Steel Ball Run, a three-lane endless runner we built in Unity. The runner moves forward on its own. You decide which lane to take and when to move. You can play with a keyboard or use a webcam and move your body to control the character. I’ll show how we got from that first three-lane track to the game you see here, then play a run.”

## 2. One run · 0:35–1:20

[Point from Choose to Run to Retry.]

“First you pick a Stand, D4C or Killer Queen, and choose keyboard or webcam. Webcam mode calibrates before you start. Once you’re running, the decisions are quick. Do I stay in this lane for the coins, or move now to get around the next obstacle? The pace picks up as you go. When you crash, you get your results and can go straight into another run.”

## 3. Your body is the controller · 1:20–2:00

[Click **Lean**, then **Match a pose**.]

“With the webcam, MediaPipe gives us body landmarks, and Unity turns those into game actions. Lean left and the runner moves left. You can also jump or crouch. We used the same tracking to make a different kind of obstacle. For this Human Tetris wall, you read the silhouette and hold that pose as the wall passes. There are punch targets too, where raising the matching arm at the right time earns a coin.”

## 4. The first playable runner · 2:00–3:00

[Show the green capsule first. Press **Next** to reveal the early character.]

“Here’s what the project looked like in September. That green capsule was our runner. The red blocks were the things you had to get around. You can already see the three lanes and the score in the corner. [Reveal.] The character changed, and we were already testing different shapes of obstacles. You can still recognize the same basic run in both shots.”

## 5. The first city build · 3:00–4:05

[Point to the coin trail and barrier. Press **Next** to reveal the city screenshot.]

“In this build, the coins lead you straight down the middle. The barrier gives you something else to watch at the same time. [Reveal.] This was the cyberpunk street test. We had buildings next to the track and neon lane markings, and you can see the separate track pieces in the middle of the scene. Watch what happens to that same view in the next slide.”

[Advance to Morioh and let the comparison land.]

## 6. Designing Morioh · 4:05–5:30

[Start with **Street composition**. Click **Painted lavender sky**, then **Drifting cloud layers** or **Painted hills + 3D town**.]

“Subway Surfers was the reference for the three-lane rhythm and those quick decisions about coins and obstacles. JoJo’s Bizarre Adventure gave us Morioh, the playable Stands, and ideas for hazards like Menacing Road Signs and Sheer Heart Attack.

“For our version of Morioh, we built the street in 3D and layered the distance behind it. Here’s the painted lavender sky, and here are the separate clouds. The nearer ones drift faster than the distant ones. The painted hills and town silhouette give the street a horizon. When you run, you can see the town moving past you while the lane markings still tell you where you can go.”

## 7. After the crash · 5:30–6:10

[Point to the example crash and tip.]

“The results give you a score, distance, coins, and time. We also added Stand Coach. If I hit a road sign, for example, my Stand can tell me what happened and suggest moving toward an open lane sooner. I’ll let you hear it during the demo.”

## 8. How Stand Coach picks a tip · 6:10–7:10

[Click **Run report**, **Local model**, and **Stand voice** as you speak.]

“Here’s what the AI actually does. Unity already knows how the run ended, so it builds a report with the control mode, the result, and the crash cause. It sends that report and a short list of possible tips to Qwen running locally. In this example, the model returns tip number one. The game takes that tip and uses Kokoro text-to-speech to read the feedback in the Stand’s selected voice. If the model can’t return a valid choice, the game uses the first tip on the list. This all runs offline once the game is set up.”

## 9. Live run · 7:10–12:30

[Switch to the game with the menu already open. Choose a Stand and webcam input. Stand centered for calibration.]

“I’ll start a webcam run. Watch the character as I lean across the lanes.”

[Show one deliberate lane change and clear one hazard. Let the audience see each action before you move on.]

“I’m going to let the next obstacle end the run so you can hear what the Stand says.”

[Crash deliberately. Give the coach time to finish. Show the results or captions.]

“That’s the tip for the crash you just saw. From here I can play again or go back and change the Stand or controls. Thanks. Happy to take questions.”

**If the webcam stalls:** Switch to keyboard and use a short run to show the same crash-to-coach sequence.

**If you can’t hear the coach:** Turn on captions on the results screen and point out the cause and practice tip.

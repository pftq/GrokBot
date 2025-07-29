# GrokBot
It's alive! Grok living on your computer with control of the mouse/keyboard.

https://github.com/user-attachments/assets/7bbf93ae-9a15-4f37-bfd1-0443f91cdcaa

# Summary
This Windows script will let Grok 4 roam freely on your desktop, controlling your mouse and keyboard (even without your input). You can ask it to do pretty much anything, and it'll at least try (but it is able to one-shot opening programs, typing into fields, doing system commands, etc). Grok will also retain its memories even if you close and reopen it!

The limitation right now is that Grok 4 is very slow (taking up 2 min for a response, video has the wait times truncated), but it is the only intelligent-enough version of Grok on the API that accepts images. This should improve over time from xAI's side though.

# Instructions
1. Unzip the GrokBot1-0_EXE.zip folder
2. Get an API key from https://x.ai/api
3. Save the API key to apikey.txt
4. Run GrokBot.exe - have fun!

There are settings available to configure in settings.ini but in general. For example, if Grok is running off too long without asking for your input, you can set maxNonResponse to a smaller number to force it to check in once in a while. The source code is available for those wanting to customize further or help improve things.

Bonus: You can use a ChatGPT API key as well and it'll automatically use ChatGPT, but ChatGPT is nowhere near as capable, struggling even just to click the Start menu.

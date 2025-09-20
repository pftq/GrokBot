# GrokBot

https://github.com/user-attachments/assets/7bbf93ae-9a15-4f37-bfd1-0443f91cdcaa

# Summary
This Windows script will let Grok 4 (or ChatGPT 5) roam freely on your desktop, controlling your mouse and keyboard (even without your input). You can ask it to do pretty much anything, and it'll at least try (but it is able to one-shot opening programs, typing into fields, doing system commands, etc). Grok will also retain its memories even if you close and reopen it!

The limitation right now is that these AIs are very slow (video has the wait times truncated). This should hopefully improve over time.  Grok-4-fast is already several times faster than Grok 4 and has been included from GrokBot1.2 onwards.

Despite the name, the script works with both Grok and ChatGPT and will automatically switch depending on the API Key (it was originally made for Grok before I realized they had the same API structure).  If you want a specific model, you can specify that (and other settings) in the settings.ini

# Instructions
Direct Download: https://github.com/pftq/GrokBot/releases/tag/1.2
1. Unzip the GrokBot1-2_EXE.zip folder
2. Get an API key. (Grok https://x.ai/api or ChatGPT https://platform.openai.com/api-keys )
3. Save the API key to apikey.txt
4. Right-click GrokBot.exe, go to Properties > Compatibility > Change High DPI > Check "Override High DPI Scaling Behavior" > Select "Application"
5. Right-click and run GrokBot.exe as Administrator  - have fun!

There are settings available to configure in settings.ini but in general. For example, if Grok is running off too long without asking for your input, you can set maxNonResponse to a smaller number to force it to check in once in a while (although it might be better to just ask it to check in more frequently instead to not interrupt multi-step tasks). The source code is available for those wanting to customize further or help improve things.

Bonus: You can use a ChatGPT API key as well and it'll automatically use ChatGPT. ChatGPT-4 struggled just to click the Start menu, but GPT-5 can now even draw in Paint!
https://x.com/pftq/status/1954387592985842151

Bonus 2: I almost lost my computer at the end of my test -_-
https://x.com/WaterflameMusic/status/1950262124397068721


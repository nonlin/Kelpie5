# Kelpie
Publish Main Project

Download Phonton Networking Plugin. 

When importing the Photon Networking plugin a file used and mofified called InRoomRoundTimer
located in Photon Unity Networking/UtilityScripts will be overwirrten with the default version provided
by photon. Either uncheck that file so that it doesn't get imported/overwritten or once it does get overridden 
copy over the download file from github and overwrite the orginal imported from photon. 

By this point photon networking should be installed and the modified version of InRoomRoundTimer. 

Load DoneStealth.unity scene to see main map and functioning game. 

The lightmap file size was to large to upload to github so it was ignored. 

You'll have to generate the lightmap from scratch.
To do this, simply enabled lights_baked by checking it. 

This will turn on all lights in the scene. 

To bake them, go to Window->Lighting.
Choose your lighting settings, the higher the detail the more time required to bake. 
Click to bake lights at the bottom once choosing desired settings. 
And wait! 
Once baking is done you can uncheck the lights_baked and you shoud still see the lights on. 

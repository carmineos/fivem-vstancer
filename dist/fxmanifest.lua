fx_version 'bodacious'
games { 'gta5' }
--dependency 'MenuAPI'

name 'vstancer'
author 'Neos7'
description 'A script to edit wheels of vehicles'
version 'v2.0.0'
url 'https://github.com/carmineos/fivem-vstancer'

files {
	--'@MenuAPI/MenuAPI.dll',
	'MenuAPI.dll',
	'Newtonsoft.Json.dll',
	'config.json'
}

client_scripts {
	'VStancer.Client.net.dll'
}

exports {
	"GetWheelPreset",
	"ResetWheelPreset",
	"GetFrontCamber",
	"GetRearCamber",
	"GetFrontTrackWidth",
	"GetRearTrackWidth",
	"SetFrontCamber",
	"SetRearCamber",
	"SetFrontTrackWidth",
	"SetRearTrackWidth",
	"SaveLocalPreset",
	"LoadLocalPreset",
	"DeleteLocalPreset",
	"GetLocalPresetList"
}
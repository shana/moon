/* -*- Mode: C++; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * Contact:
 *   Moonlight List (moonlight-list@lists.ximian.com)
 *
 * Copyright 2010 Novell, Inc. (http://www.novell.com)
 *
 * See the LICENSE file included with the distribution for details.
 * 
 */

#include <config.h>

#include "consent.h"
#include "uri.h"
#include "pal.h"
#include "runtime.h"
#include "deployment.h"
#include "moonlightconfiguration.h"

namespace Moonlight {

static const char *consent_name[] = {
	"clipboard",
	"fullscreen",
	"capture"
};

static const char *consent_description[] = {
	"Clipboard",
	"Full-screen: stay full-screen when unfocused",
	"Webcam and Microphone"
};

char*
Consent::GeneratePermissionConfigurationKey (MoonConsentType consent, const char *website)
{
	const char *name = GetConsentName (consent);
	if (name == NULL)
		return NULL;
	return g_strdup_printf ("%s-%s", website, name);
}

const char *
Consent::GetConsentName (MoonConsentType consent)
{
	if (consent < 0 || consent >= MOON_CONSENT_LAST)
		return NULL;

	return consent_name[consent];
}

const char *
Consent::GetConsentDescription (MoonConsentType consent)
{
	if (consent < 0 || consent >= MOON_CONSENT_LAST)
		return NULL;

	return consent_description[consent];
}

MoonConsentType
Consent::GetConsentType (const char *name)
{
	for (int i = 0; i < MOON_CONSENT_LAST; i ++)
		if (!g_ascii_strcasecmp (name, consent_name[i]))
			return (MoonConsentType)i;

	return (MoonConsentType)-1;
}

bool
Consent::PromptUserFor (MoonConsentType consent)
{
	Surface *surface = Deployment::GetCurrent ()->GetSurface ();
	const char *source = surface->GetSourceLocation ();

	Uri *uri = new Uri ();
	uri->Parse (source);

	char *website = ((uri->GetPort () > 0)
			 ? g_strdup_printf ("%s://%s:%d",
					    uri->GetScheme(),
					    uri->GetHost(),
					    uri->GetPort())
			 : g_strdup_printf ("%s://%s",
					    uri->GetScheme(),
					    uri->GetHost()));

	const char *question, *detail;

	switch (consent) {
	case MOON_CONSENT_CLIPBOARD:
		question = "Do you want to allow this application to access your clipboard?";
		detail = "If you allow this, the application can copy data to and from the Clipboard as long as the application is running.";
		break;
	case MOON_CONSENT_FULLSCREEN_PINNING:
		question = "[fullscreen question here]"; // FIXME
		detail = "[fullscreen detail here]"; // FIXME
		break;
	case MOON_CONSENT_CAPTURE:
		question = "Do you want to allow this application to access your webcam and microphone?";
		detail = "If you allow this, the application can capture video and audio as long as the application is running.";
		break;
	case MOON_CONSENT_LAST:
		// should't be reached...
		question = detail = NULL;
		break;
	}

	return PromptUserFor (consent, question, detail, website);
}

bool
Consent::PromptUserFor (MoonConsentType consent, const char *question, const char *detail, const char *website)
{
	bool remember = false;
	bool rv = false; 

	MoonlightConfiguration configuration;

	char *config_key = GeneratePermissionConfigurationKey (consent, website);
	if (configuration.HasKey ("Permissions", config_key)) {
		rv = configuration.GetBooleanValue ("Permissions", config_key);
		g_free (config_key);
		return rv;
	}


	MoonWindowingSystem *windowing_system = runtime_get_windowing_system ();

	rv = windowing_system->ShowConsentDialog (question,
						  detail,
						  website,
						  &remember);

	if (remember) {
		configuration.SetBooleanValue ("Permissions", config_key, rv);
		configuration.Save ();
	}

	g_free (config_key);
	return rv;
}

};

#include "vdriver.h"

//Show startup screen
#define SHOW_TITLECARD

void startup();
#ifdef SHOW_TITLECARD
void titlecard();
#endif

void main()
{
	startup();
#ifdef SHOW_TITLECARD
	titlecard();
#endif
}

void startup()
{
	vdriver_startup();
}

#ifdef SHOW_TITLECARD
void titlecard()
{
	
}
#endif
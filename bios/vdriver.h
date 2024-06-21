#ifndef VDRIVER
#define VDRIVER

#define GPU_ARG_MEM0 0x30000000
#define GPU_ARG_MEM1 0x30000001
#define GPU_ARG_MEM2 0x30000002
#define GPU_ARG_MEM3 0x30000003
#define GPU_ARG_MEM4 0x30000004
#define GPU_ARG_MEM5 0x30000005
#define GPU_ARG_MEM6 0x30000006
#define GPU_ARG_MEM7 0x30000007

#define COLOR_WHITE 0xffffffff
#define COLOR_RED 0xff0000ff
#define COLOR_GREEN 0x0ff00ff
#define COLOR_BLUE 0x0000ffff
#define COLOR_BLACK 0x000000ff

void vdriver_startup()
{
	int ignored_var = 0;
	__asm__
	{
		"in {ignored_var}, VDR_Startup"
	}
}

void vdriver_kill()
{
	int ignored_var = 0;
	__asm__
	{
		"in {ignored_var}, VDR_Kill"
	}
}

void vdriver_flush()
{
	int ignored_var = 0;
	__asm__
	{
		"in {ignored_var}, VDR_Flush"
	}
}

int vdriver_create_framebuffer(int width, int height, int format)
{
	int result = 0;
	__asm__
	{
		"mov $GPU_ARG_MEM0, {width}"
		"mov $GPU_ARG_MEM1, {height}"
		"mov $GPU_ARG_MEM2, {format}"
		"in R0, VDR_CreateFramebuffer"
		"mov {result}, R0"
	}
	return result;
}

void vdriver_define_framebuffer(int id)
{
	__asm__
	{
		"mov R0, {id}"
		"out VDR_DefineFramebuffer, R0"
	}
}

void vdriver_clear(int color)
{
	__asm__
	{
		"mov R0, {color}"
		"out VDR_Clear, R0"
	}
}

#endif
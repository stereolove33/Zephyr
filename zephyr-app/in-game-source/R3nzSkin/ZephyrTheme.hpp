#pragma once
#include "imgui/imgui.h"
inline void ApplyZephyrTheme() noexcept
{
    auto colors = ImGui::GetStyle().Colors;
    for (int index = 0; index < ImGuiCol_COUNT; ++index) {
        auto& color = colors[index];
        float gray = (color.x + color.y + color.z) / 3.0f;
        color.x = color.y = color.z = gray;
    }
    colors[ImGuiCol_Text] = ImVec4(1.000f, 1.000f, 1.000f, 1.000f);
    colors[ImGuiCol_TextDisabled] = ImVec4(0.500f, 0.500f, 0.500f, 1.000f);
    colors[ImGuiCol_WindowBg] = ImVec4(0.025f, 0.025f, 0.025f, 1.000f);
    colors[ImGuiCol_ChildBg] = ImVec4(0.000f, 0.000f, 0.000f, 0.000f);
    colors[ImGuiCol_PopupBg] = ImVec4(0.045f, 0.045f, 0.045f, 0.940f);
    colors[ImGuiCol_Border] = ImVec4(0.360f, 0.360f, 0.360f, 1.000f);
    colors[ImGuiCol_BorderShadow] = ImVec4(0.000f, 0.000f, 0.000f, 0.000f);
    colors[ImGuiCol_FrameBg] = ImVec4(0.100f, 0.100f, 0.100f, 1.000f);
    colors[ImGuiCol_FrameBgHovered] = ImVec4(0.210f, 0.210f, 0.210f, 1.000f);
    colors[ImGuiCol_FrameBgActive] = ImVec4(0.300f, 0.300f, 0.300f, 1.000f);
    colors[ImGuiCol_TitleBg] = ImVec4(0.035f, 0.035f, 0.035f, 1.000f);
    colors[ImGuiCol_TitleBgActive] = ImVec4(0.060f, 0.060f, 0.060f, 1.000f);
    colors[ImGuiCol_TitleBgCollapsed] = ImVec4(0.000f, 0.000f, 0.000f, 0.510f);
    colors[ImGuiCol_MenuBarBg] = ImVec4(0.110f, 0.110f, 0.110f, 1.000f);
    colors[ImGuiCol_ScrollbarBg] = ImVec4(0.060f, 0.060f, 0.060f, 0.530f);
    colors[ImGuiCol_ScrollbarGrab] = ImVec4(0.210f, 0.210f, 0.210f, 1.000f);
    colors[ImGuiCol_ScrollbarGrabHovered] = ImVec4(0.470f, 0.470f, 0.470f, 1.000f);
    colors[ImGuiCol_ScrollbarGrabActive] = ImVec4(0.817f, 0.817f, 0.817f, 1.000f);
    colors[ImGuiCol_CheckMark] = ImVec4(1.000f, 1.000f, 1.000f, 1.000f);
    colors[ImGuiCol_SliderGrab] = ImVec4(0.780f, 0.780f, 0.780f, 1.000f);
    colors[ImGuiCol_SliderGrabActive] = ImVec4(1.000f, 1.000f, 1.000f, 1.000f);
    colors[ImGuiCol_Button] = ImVec4(0.140f, 0.140f, 0.140f, 1.000f);
    colors[ImGuiCol_ButtonHovered] = ImVec4(0.260f, 0.260f, 0.260f, 1.000f);
    colors[ImGuiCol_ButtonActive] = ImVec4(0.360f, 0.360f, 0.360f, 1.000f);
    colors[ImGuiCol_Header] = ImVec4(0.140f, 0.140f, 0.140f, 1.000f);
    colors[ImGuiCol_HeaderHovered] = ImVec4(0.260f, 0.260f, 0.260f, 1.000f);
    colors[ImGuiCol_HeaderActive] = ImVec4(0.360f, 0.360f, 0.360f, 1.000f);
    colors[ImGuiCol_Separator] = ImVec4(0.210f, 0.210f, 0.210f, 1.000f);
    colors[ImGuiCol_SeparatorHovered] = ImVec4(0.560f, 0.560f, 0.560f, 1.000f);
    colors[ImGuiCol_SeparatorActive] = ImVec4(0.513f, 0.513f, 0.513f, 1.000f);
    colors[ImGuiCol_ResizeGrip] = ImVec4(0.210f, 0.210f, 0.210f, 1.000f);
    colors[ImGuiCol_ResizeGripHovered] = ImVec4(0.560f, 0.560f, 0.560f, 1.000f);
    colors[ImGuiCol_ResizeGripActive] = ImVec4(0.513f, 0.513f, 0.513f, 1.000f);
    colors[ImGuiCol_Tab] = ImVec4(0.085f, 0.085f, 0.085f, 1.000f);
    colors[ImGuiCol_TabHovered] = ImVec4(0.260f, 0.260f, 0.260f, 1.000f);
    colors[ImGuiCol_TabActive] = ImVec4(0.210f, 0.210f, 0.210f, 1.000f);
    colors[ImGuiCol_TabUnfocused] = ImVec4(0.107f, 0.107f, 0.107f, 0.970f);
    colors[ImGuiCol_TabUnfocusedActive] = ImVec4(0.273f, 0.273f, 0.273f, 1.000f);
    colors[ImGuiCol_PlotLines] = ImVec4(0.610f, 0.610f, 0.610f, 1.000f);
    colors[ImGuiCol_PlotLinesHovered] = ImVec4(0.593f, 0.593f, 0.593f, 1.000f);
    colors[ImGuiCol_PlotHistogram] = ImVec4(0.533f, 0.533f, 0.533f, 1.000f);
    colors[ImGuiCol_PlotHistogramHovered] = ImVec4(0.533f, 0.533f, 0.533f, 1.000f);
    colors[ImGuiCol_TextSelectedBg] = ImVec4(0.610f, 0.610f, 0.610f, 0.350f);
    colors[ImGuiCol_DragDropTarget] = ImVec4(0.667f, 0.667f, 0.667f, 0.900f);
    colors[ImGuiCol_NavHighlight] = ImVec4(0.610f, 0.610f, 0.610f, 1.000f);
    colors[ImGuiCol_NavWindowingHighlight] = ImVec4(1.000f, 1.000f, 1.000f, 0.700f);
    colors[ImGuiCol_NavWindowingDimBg] = ImVec4(0.800f, 0.800f, 0.800f, 0.200f);
    colors[ImGuiCol_ModalWindowDimBg] = ImVec4(0.800f, 0.800f, 0.800f, 0.350f);
}

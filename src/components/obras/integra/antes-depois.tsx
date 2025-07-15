"use client";
import dynamic from "next/dynamic";
const ReactCompareImage = dynamic(() => import("react-compare-image"), { ssr: false });
import { ArrowSlider } from "../../../../public/img/svg/icons"; 
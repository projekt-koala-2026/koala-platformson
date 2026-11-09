interface KoalaIllustrationProps {
    src: string;
}

const KoalaIllustration = ({ src }: KoalaIllustrationProps) => (
    <div
        aria-hidden="true"
        className="flex h-24 w-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl bg-brand-soft p-1 sm:h-32 sm:w-32 sm:p-1.5"
    >
        <img
            src={src}
            alt=""
            className="koala-illustration block h-full w-full min-w-0 object-contain"
        />
    </div>
);

export default KoalaIllustration;

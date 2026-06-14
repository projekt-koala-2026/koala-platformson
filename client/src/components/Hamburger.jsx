import { useState } from "react";
import Button from "./Button";
import styles from "./Hamburger.module.css";

const Hamburger = ({ options }) => {
    const [isMenu, setIsMenu] = useState(false);

    return (
        <div className={`${styles.hamburgerContainer} ${isMenu ? styles.active : ""}`}>
            <div className={`${styles.hamburgerMenu} ${isMenu ? styles.show : ""}`}>
                <div className={styles.menuHeaderSpace}></div>
                <div className={styles.menuContent}>
                    {options &&
                        options.map((item, idx) => (
                            <Button
                                key={"hamburger-item-" + idx}
                                text={item[0]}
                                onClick={item[1]}
                            />
                        ))}
                </div>
            </div>
            <div className={styles.hamburgerCircle} onClick={() => setIsMenu((prev) => !prev)}>
                <div className={styles.burgerIcon}>
                    <span></span>
                    <span></span>
                    <span></span>
                </div>
            </div>
        </div>
    );
};

export default Hamburger;
